using Common.Tools;

namespace Persistence;

[MongoDB.Entities.Collection("UserProfiles")]
sealed class UserProfileEntity : Entity
{
    public string Email { get; init; } = null!;
    public string NormalizedEmail { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public UserProfileStatus Status { get; set; } = UserProfileStatus.Deactivated;
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; init; }

    public static UserProfileEntity Create(string email, string displayName, DateTime now)
        => new()
        {
            Email = email.Trim(),
            NormalizedEmail = email.NormalizeForLookup(),
            DisplayName = displayName.Trim(),
            CreatedAt = now
        };
}

enum UserProfileStatus
{
    Active,
    Deactivated,
    Suspended
}