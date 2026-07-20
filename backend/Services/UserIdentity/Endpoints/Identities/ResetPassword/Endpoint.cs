using Identities;
using Microsoft.AspNetCore.Identity;

namespace Identities.ResetPassword;

sealed class Endpoint(
    IUserIdentityStore identities,
    IPasswordResetTokenStore resetTokens,
    IPasswordHasher<UserIdentityEntity> hasher)
    : Endpoint<Request, string>
{
    internal const string SuccessMessage = PasswordReset.ResetSuccessMessage;
    internal const string InvalidOrExpiredMessage = PasswordReset.InvalidOrExpiredMessage;

    public override void Configure()
    {
        Post("identities/reset-password/{ResetCode}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.ResetCode))
            ThrowError(InvalidOrExpiredMessage);

        var tokenHash = PasswordResetTokenEntity.HashCode(r.ResetCode);
        var token = await resetTokens.FindByTokenHashAsync(tokenHash, ct);
        var now = DateTime.UtcNow;

        if (token is null || token.ExpireAt <= now)
        {
            if (token is not null)
                await resetTokens.DeleteByUserIdentityIdAsync(token.UserIdentityId, ct);

            ThrowError(InvalidOrExpiredMessage);
        }

        var identity = await identities.FindByIdAsync(token.UserIdentityId, ct);

        if (identity is null || identity.Status != UserIdentityStatus.Active)
        {
            await resetTokens.DeleteByUserIdentityIdAsync(token.UserIdentityId, ct);
            ThrowError(InvalidOrExpiredMessage);
        }

        var passwordHash = hasher.HashPassword(identity, r.Password);
        await identities.UpdatePasswordHashAsync(identity.ID, passwordHash, ct);
        await resetTokens.DeleteByUserIdentityIdAsync(identity.ID, ct);

        await Send.OkAsync(SuccessMessage, ct);
    }
}
