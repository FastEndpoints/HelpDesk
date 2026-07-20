using Contracts.UserProfile;

namespace Subscriptions.UserProfile.Registration.Tests;

public class Cases
{
    [Fact]
    public async Task Upserts_DisplayName_From_UserProfileRegisteredEvent()
    {
        var store = new FakeDisplayNameStore();
        var handler = new UserProfileRegisteredEventHandler(store);
        var registeredAt = DateTime.UtcNow.AddMinutes(-5);
        var eventModel = new UserProfileRegisteredEvent(
            "profile-id",
            "identity-id-registered",
            "user@example.com",
            "Default Name",
            registeredAt);

        await handler.HandleAsync(eventModel, CancellationToken.None);

        var stored = store.Entries.Single();
        stored.UserIdentityId.ShouldBe(eventModel.UserIdentityId);
        stored.DisplayName.ShouldBe(eventModel.DisplayName);
        stored.UpdatedAt.ShouldBe(registeredAt);
    }

    sealed class FakeDisplayNameStore : IDisplayNameStore
    {
        public List<(string UserIdentityId, string DisplayName, DateTime UpdatedAt)> Entries { get; } = [];

        public Task UpsertAsync(string userIdentityId, string displayName, DateTime updatedAt, CancellationToken ct)
        {
            Entries.RemoveAll(e => e.UserIdentityId == userIdentityId);
            Entries.Add((userIdentityId, displayName, updatedAt));

            return Task.CompletedTask;
        }

        public Task<string?> FindAsync(string userIdentityId, CancellationToken ct)
        {
            var match = Entries.SingleOrDefault(e => e.UserIdentityId == userIdentityId);

            return Task.FromResult(match.UserIdentityId is null ? null : match.DisplayName);
        }
    }
}
