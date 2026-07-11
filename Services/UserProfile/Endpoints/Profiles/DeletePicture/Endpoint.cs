using System.Security.Claims;
using Common.Tools;

namespace Endpoints.Profiles.DeletePicture;

sealed class Endpoint(IUserProfileStore profiles, IProfilePictureStorage storage) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Delete("profiles/me/picture");
        AccessControl("Profiles_Delete_Own_Picture", Apply.ToThisEndpoint, PermissionGroups.User);
    }

    public override async Task HandleAsync(CancellationToken ct)
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

        var previousKey = profile.PictureObjectKey;

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            var updated = await TryClearPictureKeyAsync(profiles, userIdentityId, previousKey, ct);

            if (!updated)
            {
                AddError("The profile picture changed concurrently. Please retry.");
                await Send.ErrorsAsync(409, ct);

                return;
            }

            try
            {
                await storage.DeleteAsync(previousKey, ct);
            }
            catch
            {
                await profiles.TryUpdatePictureObjectKeyAsync(userIdentityId, null, previousKey, CancellationToken.None);
                throw;
            }
        }

        await Send.OkAsync(
            new()
            {
                Id = profile.ID,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                Status = profile.Status.ToString(),
                PictureUrl = null
            },
            ct);
    }

    internal static async Task<bool> TryClearPictureKeyAsync(IUserProfileStore profiles,
                                                              string userIdentityId,
                                                              string previousKey,
                                                              CancellationToken ct)
    {
        try
        {
            return await profiles.TryUpdatePictureObjectKeyAsync(userIdentityId, previousKey, null, ct);
        }
        catch
        {
            var verified = false;
            var cleared = false;

            try
            {
                var current = await profiles.FindByUserIdentityIdAsync(userIdentityId, CancellationToken.None);
                verified = true;
                cleared = current is not null && current.PictureObjectKey is null;
            }
            catch
            {
                // Do not delete the public file while Mongo state remains unknown.
            }

            if (!verified || !cleared)
                throw;

            return true;
        }
    }
}