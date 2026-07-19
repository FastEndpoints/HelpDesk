using Common.Tools;
using Contracts.UserIdentity;
using Identities;
using Microsoft.Extensions.Options;

namespace Identities.ResendVerification;

sealed class Endpoint(IUserIdentityStore store, IOptions<UserIdentitySettings> settings) : Endpoint<Request, string>
{
    internal const string SuccessMessage = "If an account needs verification, we sent a link.";
    internal static readonly TimeSpan ResendCooldown = VerificationResend.Cooldown;

    public override void Configure()
    {
        Post("identities/resend-verification");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var frontendBaseUrl = settings.Value.UserIdentity.FrontendBaseUrl?.Trim();

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            ThrowError("Frontend base URL is not configured.");

        var identity = await store.FindByEmailAsync(r.Email.NormalizeForLookup(), ct);
        var now = DateTime.UtcNow;

        if (identity is { Status: UserIdentityStatus.Deactivated } && now - identity.VerificationIssuedAt >= ResendCooldown)
        {
            var verificationCode = UserIdentityEntity.CreateVerificationCode();
            await store.ReplaceVerificationCodeAsync(identity.ID, verificationCode, now, ct);

            var baseUrl = frontendBaseUrl.TrimEnd('/');

            new UserIdentityVerificationIssuedEvent(
                    identity.ID,
                    identity.Email,
                    verificationCode,
                    baseUrl,
                    now)
                .Broadcast();
        }

        await Send.OkAsync(SuccessMessage, ct);
    }
}