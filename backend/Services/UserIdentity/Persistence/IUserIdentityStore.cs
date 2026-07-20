namespace Persistence;

interface IUserIdentityStore
{
    Task<UserIdentityEntity?> FindByIdAsync(string id, CancellationToken ct);
    Task<UserIdentityEntity?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<UserIdentityEntity?> FindByVerificationCodeAsync(string verificationCode, CancellationToken ct);
    Task CreateAsync(UserIdentityEntity identity, CancellationToken ct);
    Task ActivateAsync(string id, CancellationToken ct);
    Task ReplaceVerificationCodeAsync(string id, string verificationCode, DateTime issuedAt, CancellationToken ct);
    Task UpdatePasswordHashAsync(string id, string passwordHash, CancellationToken ct);
}