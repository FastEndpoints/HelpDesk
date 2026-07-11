using System.Security.Claims;
using Common.Tools;
using ProfilePictures;

namespace Endpoints.Profiles.UpdateCurrent;

sealed class Endpoint(IUserProfileStore profiles, IProfilePictureStorage pictures) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("profiles/me");
        AccessControl("Profiles_Update_Own", Apply.ToThisEndpoint, PermissionGroups.User);
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

        var displayName = r.DisplayName.Trim();
        await profiles.UpdateDisplayNameAsync(userIdentityId, displayName, ct);

        await Send.OkAsync(
            new()
            {
                Id = profile.ID,
                Email = profile.Email,
                DisplayName = displayName,
                Status = profile.Status.ToString(),
                PictureUrl = pictures.BuildPublicUrl(profile.PictureObjectKey, HttpContext.Request)
            },
            ct);
    }
}
