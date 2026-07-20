namespace Persistence;

interface IPasswordResetTokenStore
{
    Task<PasswordResetTokenEntity?> FindByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<PasswordResetTokenEntity?> FindLatestByUserIdentityIdAsync(string userIdentityId, CancellationToken ct);
    Task CreateAsync(PasswordResetTokenEntity token, CancellationToken ct);
    Task DeleteByUserIdentityIdAsync(string userIdentityId, CancellationToken ct);
}
