using Common.Tools;
using Contracts.UserIdentity;
using Identities;
using Microsoft.Extensions.Options;

namespace Identities.ForgotPassword;

sealed class Endpoint(
    IUserIdentityStore identities,
    IPasswordResetTokenStore resetTokens,
    IOptions<UserIdentitySettings> settings)
    : Endpoint<Request, string>
{
    internal const string SuccessMessage = PasswordReset.RequestSuccessMessage;

    public override void Configure()
    {
        Post("identities/forgot-password");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var frontendBaseUrl = settings.Value.UserIdentity.FrontendBaseUrl?.Trim();

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            ThrowError("Frontend base URL is not configured.");

        // Always set so the BFF can seed a client timer without account enumeration.
        // Unknown / non-Active: full cooldown (opaque). Active: remaining from latest token.
        var availableInSeconds = (int)PasswordReset.RequestCooldown.TotalSeconds;
        var identity = await identities.FindByEmailAsync(r.Email.NormalizeForLookup(), ct);
        var now = DateTime.UtcNow;

        if (identity is { Status: UserIdentityStatus.Active })
        {
            var latest = await resetTokens.FindLatestByUserIdentityIdAsync(identity.ID, ct);
            var withinCooldown = latest is not null && now - latest.CreatedAt < PasswordReset.RequestCooldown;

            if (!withinCooldown)
            {
                await resetTokens.DeleteByUserIdentityIdAsync(identity.ID, ct);

                var rawCode = PasswordResetTokenEntity.CreateRawCode();
                var token = PasswordResetTokenEntity.Create(
                    identity.ID,
                    identity.NormalizedEmail,
                    rawCode,
                    now,
                    PasswordReset.TokenLifetime);

                await resetTokens.CreateAsync(token, ct);

                var baseUrl = frontendBaseUrl.TrimEnd('/');

                new UserIdentityPasswordResetIssuedEvent(
                        identity.ID,
                        identity.Email,
                        rawCode,
                        baseUrl,
                        now)
                    .Broadcast();

                availableInSeconds = PasswordReset.AvailableInSeconds(token.CreatedAt, now);
            }
            else
                availableInSeconds = PasswordReset.AvailableInSeconds(latest!.CreatedAt, now);
        }

        HttpContext.Response.Headers[PasswordReset.AvailableInHeaderName] = availableInSeconds.ToString();
        await Send.OkAsync(SuccessMessage, ct);
    }
}