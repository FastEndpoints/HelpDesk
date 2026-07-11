using Common.Tools;

namespace Persistence;

[MongoDB.Entities.Collection("UserProfiles")]
sealed class UserProfileEntity : Entity
{
    public string UserIdentityId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string NormalizedEmail { get; init; } = null!;
    public string DisplayName { get; set; } = null!;
    public UserProfileStatus Status { get; set; } = UserProfileStatus.Deactivated;
    public bool EmailVerified { get; set; }
    /// <summary>
    /// Relative object key under picture storage root (e.g. profiles/{userIdentityId}/{version}.jpg). Null when no picture.
    /// </summary>
    public string? PictureObjectKey { get; set; }
    public DateTime CreatedAt { get; init; }

    public static UserProfileEntity Create(string userIdentityId, string email, string displayName, DateTime now)
        => new()
        {
            UserIdentityId = userIdentityId,
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