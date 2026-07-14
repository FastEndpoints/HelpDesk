using Contracts.UserIdentity;

namespace Subscriptions.UserIdentity.Verification;

sealed class UserIdentityVerifiedEventHandler(IUserProfileStore profiles)
    : IEventHandler<UserIdentityVerifiedEvent>
{
    public Task HandleAsync(UserIdentityVerifiedEvent eventModel, CancellationToken ct)
        => profiles.ActivateByUserIdentityIdAsync(eventModel.UserIdentityId, ct);
}
