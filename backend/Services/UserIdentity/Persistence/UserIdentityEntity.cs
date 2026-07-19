using Common.Tools;

namespace Persistence;

[MongoDB.Entities.Collection("UserIdentities")]
sealed class UserIdentityEntity : Entity
{
    public string Email { get; init; } = null!;
    public string NormalizedEmail { get; init; } = null!;
    public string PasswordHash { get; init; } = null!;
    public string VerificationCode { get; init; } = null!;
    public DateTime VerificationIssuedAt { get; init; }
    public UserIdentityStatus Status { get; init; } = UserIdentityStatus.Deactivated;
    public DateTime CreatedAt { get; init; }
    public string[] Groups { get; init; } = [];

    public static UserIdentityEntity Create(string email, string passwordHash, DateTime now)
        => new()
        {
            Email = email.Trim(),
            NormalizedEmail = email.NormalizeForLookup(),
            PasswordHash = passwordHash,
            VerificationCode = CreateVerificationCode(),
            Groups = [.. PermissionGroups.Defaults],
            CreatedAt = now,
            VerificationIssuedAt = now
        };

    internal static string CreateVerificationCode()
        => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}

enum UserIdentityStatus
{
    Active,
    Locked,
    Deactivated
}