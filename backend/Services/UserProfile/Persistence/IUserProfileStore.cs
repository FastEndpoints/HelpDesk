namespace Persistence;

interface IUserProfileStore
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct);
    Task<UserProfileEntity?> FindByUserIdentityIdAsync(string userIdentityId, CancellationToken ct);
    Task CreateAsync(UserProfileEntity profile, CancellationToken ct);
    Task ActivateByUserIdentityIdAsync(string userIdentityId, CancellationToken ct);
    Task UpdateDisplayNameAsync(string userIdentityId, string displayName, CancellationToken ct);
    Task<bool> TryUpdatePictureObjectKeyAsync(string userIdentityId, string? expectedPictureObjectKey, string? pictureObjectKey, CancellationToken ct);
}