using FastEndpoints;

namespace Contracts.UserProfile;

public sealed record UserProfileRegisteredEvent(
    string UserProfileId,
    string Email,
    string DisplayName,
    string VerificationCode,
    string BaseUrl,
    DateTime RegisteredAt) : IEvent;