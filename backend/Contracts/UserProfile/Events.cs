using FastEndpoints;

namespace Contracts.UserProfile;

public sealed record UserProfileRegisteredEvent(
    string UserProfileId,
    string UserIdentityId,
    string Email,
    string DisplayName,
    DateTime RegisteredAt) : IEvent;

public sealed record UserProfileDisplayNameUpdatedEvent(
    string UserProfileId,
    string UserIdentityId,
    string DisplayName,
    DateTime UpdatedAt) : IEvent;
