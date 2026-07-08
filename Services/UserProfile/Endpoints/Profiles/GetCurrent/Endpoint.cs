using System.Security.Claims;

namespace Endpoints.Profiles.GetCurrent;

sealed class Endpoint(IUserProfileStore profiles) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("profiles/me");
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

        await Send.OkAsync(
            new()
            {
                Id = profile.ID,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                Status = profile.Status.ToString()
            },
            ct);
    }
}