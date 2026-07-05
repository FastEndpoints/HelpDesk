using FastEndpoints;

namespace Contracts.UserIdentity;

public sealed record UserIdentityRegisteredEvent(
    string UserIdentityId,
    string Email,
    string VerificationCode,
    string BaseUrl,
    DateTime RegisteredAt) : IEvent;

public sealed record UserIdentityVerifiedEvent(
    string UserIdentityId,
    string Email,
    DateTime VerifiedAt) : IEvent;
