namespace Persistence;

interface IUserIdentityStore
{
    Task<UserIdentityEntity?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<UserIdentityEntity?> FindByVerificationCodeAsync(string verificationCode, CancellationToken ct);
    Task CreateAsync(UserIdentityEntity identity, CancellationToken ct);
    Task ActivateAsync(string id, CancellationToken ct);
}