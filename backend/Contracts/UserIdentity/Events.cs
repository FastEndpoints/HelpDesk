using FastEndpoints;

namespace Contracts.UserIdentity;

public sealed record UserIdentityRegisteredEvent(
    string UserIdentityId,
    string Email,
    DateTime RegisteredAt) : IEvent;

public sealed record UserIdentityVerificationIssuedEvent(
    string UserIdentityId,
    string Email,
    string VerificationCode,
    string BaseUrl,
    DateTime IssuedAt) : IEvent;

public sealed record UserIdentityVerifiedEvent(
    string UserIdentityId,
    string Email,
    DateTime VerifiedAt) : IEvent;

public sealed record UserIdentityPasswordResetIssuedEvent(
    string UserIdentityId,
    string Email,
    string ResetCode,
    string BaseUrl,
    DateTime IssuedAt) : IEvent;
