using System.Security.Cryptography;
using System.Text;

namespace Persistence;

[MongoDB.Entities.Collection("PasswordResetTokens")]
sealed class PasswordResetTokenEntity : Entity
{
    public string UserIdentityId { get; init; } = null!;
    public string TokenHash { get; init; } = null!;
    public string NormalizedEmail { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpireAt { get; init; }

    public static PasswordResetTokenEntity Create(
        string userIdentityId,
        string normalizedEmail,
        string rawCode,
        DateTime now,
        TimeSpan lifetime)
        => new()
        {
            UserIdentityId = userIdentityId,
            NormalizedEmail = normalizedEmail,
            TokenHash = HashCode(rawCode),
            CreatedAt = now,
            ExpireAt = now.Add(lifetime)
        };

    internal static string CreateRawCode()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    internal static string HashCode(string rawCode)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode)));
}
