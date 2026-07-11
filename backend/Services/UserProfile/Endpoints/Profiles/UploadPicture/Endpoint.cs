using System.Security.Claims;
using Common.Tools;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Endpoints.Profiles.UploadPicture;

sealed class Endpoint(IUserProfileStore profiles,
                      IProfilePictureStorage storage,
                      IProfilePictureProcessor processor,
                      IOptions<UserProfileSettings> options)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("profiles/me/picture");
        AllowFileUploads();
        MaxRequestBodySize(options.Value.UserProfile.ProfilePictures.MaxUploadBytes + UserProfileSettings.ProfilePictureSettings.MultipartOverheadBytes);
        AccessControl("Profiles_Upload_Own_Picture", Apply.ToThisEndpoint, PermissionGroups.User);
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var userIdentityId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userIdentityId))
        {
            await Send.UnauthorizedAsync(ct);

            return;
        }

        var profile = await profiles.FindByUserIdentityIdAsync(userIdentityId, ct);

        if (profile is null)
        {
            await Send.NotFoundAsync(ct);

            return;
        }

        if (profile.Status != UserProfileStatus.Active)
        {
            await Send.ForbiddenAsync(ct);

            return;
        }

        ProcessedProfilePicture processed;

        try
        {
            await using var upload = r.File.OpenReadStream();
            processed = await processor.ProcessAsync(upload, ct);
        }
        catch (InvalidImageFormatException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);

            return;
        }
        catch (UnknownImageFormatException)
        {
            AddError("Only PNG and JPG images are allowed.");
            await Send.ErrorsAsync(cancellation: ct);

            return;
        }
        catch (InvalidImageContentException)
        {
            AddError("The picture file is corrupt or invalid.");
            await Send.ErrorsAsync(cancellation: ct);

            return;
        }

        await using var processedStream = new MemoryStream(processed.Bytes, writable: false);
        var newKey = await storage.PutAsync(userIdentityId, processedStream, processed.Extension, ct);
        var previousKey = profile.PictureObjectKey;
        var updated = await TryCommitPictureKeyAsync(profiles, storage, userIdentityId, previousKey, newKey, ct);

        if (!updated)
        {
            AddError("The profile picture changed concurrently. Please retry.");
            await Send.ErrorsAsync(409, ct);

            return;
        }

        if (!string.IsNullOrWhiteSpace(previousKey) &&
            !string.Equals(previousKey, newKey, StringComparison.Ordinal))
            await DeletePreviousOrRollbackAsync(profiles, storage, userIdentityId, previousKey, newKey, ct);

        await Send.OkAsync(
            new()
            {
                Id = profile.ID,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                Status = profile.Status.ToString(),
                PictureUrl = storage.BuildPublicUrl(newKey, HttpContext.Request)
            },
            ct);
    }

    internal static async Task<bool> TryCommitPictureKeyAsync(IUserProfileStore profiles,
                                                               IProfilePictureStorage storage,
                                                               string userIdentityId,
                                                               string? previousKey,
                                                               string newKey,
                                                               CancellationToken ct)
    {
        try
        {
            var updated = await profiles.TryUpdatePictureObjectKeyAsync(userIdentityId, previousKey, newKey, ct);

            if (!updated)
                await storage.DeleteAsync(newKey, CancellationToken.None);

            return updated;
        }
        catch
        {
            var verified = false;
            var updated = false;

            try
            {
                var current = await profiles.FindByUserIdentityIdAsync(userIdentityId, CancellationToken.None);
                verified = true;
                updated = current?.PictureObjectKey == newKey;
            }
            catch
            {
                // Keep the file when Mongo state cannot be verified; deleting could break a committed update.
            }

            if (verified && !updated)
                await storage.DeleteAsync(newKey, CancellationToken.None);

            if (!updated)
                throw;

            return true;
        }
    }

    internal static async Task DeletePreviousOrRollbackAsync(IUserProfileStore profiles,
                                                              IProfilePictureStorage storage,
                                                              string userIdentityId,
                                                              string previousKey,
                                                              string newKey,
                                                              CancellationToken ct)
    {
        try
        {
            await storage.DeleteAsync(previousKey, ct);
        }
        catch
        {
            var rolledBack = false;

            try
            {
                rolledBack = await profiles.TryUpdatePictureObjectKeyAsync(userIdentityId, newKey, previousKey, CancellationToken.None);
            }
            catch
            {
                try
                {
                    var current = await profiles.FindByUserIdentityIdAsync(userIdentityId, CancellationToken.None);
                    rolledBack = current?.PictureObjectKey == previousKey;
                }
                catch
                {
                    // Keep both files when Mongo state cannot be verified.
                }
            }

            if (rolledBack)
            {
                try
                {
                    await storage.DeleteAsync(newKey, CancellationToken.None);
                }
                catch
                {
                    // Metadata safely references the previous file; orphan cleanup can be retried operationally.
                }
            }

            throw;
        }
    }
}