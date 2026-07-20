using Contracts.UserProfile;

namespace Subscriptions.UserProfile.DisplayNameUpdated;

sealed class UserProfileDisplayNameUpdatedEventHandler(IDisplayNameStore displayNames)
    : IEventHandler<UserProfileDisplayNameUpdatedEvent>
{
    public Task HandleAsync(UserProfileDisplayNameUpdatedEvent eventModel, CancellationToken ct)
        => displayNames.UpsertAsync(
            eventModel.UserIdentityId,
            eventModel.DisplayName,
            eventModel.UpdatedAt,
            ct);
}