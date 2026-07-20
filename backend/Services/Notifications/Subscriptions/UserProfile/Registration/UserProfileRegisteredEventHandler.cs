using Contracts.UserProfile;

namespace Subscriptions.UserProfile.Registration;

sealed class UserProfileRegisteredEventHandler(IDisplayNameStore displayNames)
    : IEventHandler<UserProfileRegisteredEvent>
{
    public Task HandleAsync(UserProfileRegisteredEvent eventModel, CancellationToken ct)
        => displayNames.UpsertAsync(
            eventModel.UserIdentityId,
            eventModel.DisplayName,
            eventModel.RegisteredAt,
            ct);
}
