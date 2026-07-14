using Contracts.UserIdentity;

namespace Subscriptions.UserIdentity.Verification.Tests;

public class Cases
{
    [Fact]
    public async Task Activates_Profile_From_UserIdentityVerifiedEvent()
    {
        var store = new FakeUserProfileStore();
        var profile = UserProfileEntity.Create("identity-id", "user@example.com", "user", DateTime.UtcNow);
        await store.CreateAsync(profile, CancellationToken.None);

        var handler = new UserIdentityVerifiedEventHandler(store);
        var eventModel = new UserIdentityVerifiedEvent("identity-id", " USER@example.com ", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, CancellationToken.None);

        profile.Status.ShouldBe(UserProfileStatus.Active);
        profile.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task Activates_By_UserIdentityId_Even_When_Event_Email_Differs()
    {
        var store = new FakeUserProfileStore();
        var profile = UserProfileEntity.Create("identity-id", "stored@example.com", "stored", DateTime.UtcNow);
        await store.CreateAsync(profile, CancellationToken.None);

        var handler = new UserIdentityVerifiedEventHandler(store);
        var eventModel = new UserIdentityVerifiedEvent("identity-id", "other@example.com", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, CancellationToken.None);

        profile.Status.ShouldBe(UserProfileStatus.Active);
        profile.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task Does_Not_Activate_Profile_For_Different_UserIdentityId()
    {
        var store = new FakeUserProfileStore();
        var profileA = UserProfileEntity.Create("identity-a", "a@example.com", "a", DateTime.UtcNow);
        var profileB = UserProfileEntity.Create("identity-b", "b@example.com", "b", DateTime.UtcNow);
        await store.CreateAsync(profileA, CancellationToken.None);
        await store.CreateAsync(profileB, CancellationToken.None);

        var handler = new UserIdentityVerifiedEventHandler(store);
        // Email matches B, but correlation must use identity A only.
        var eventModel = new UserIdentityVerifiedEvent("identity-a", "b@example.com", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, CancellationToken.None);

        profileA.Status.ShouldBe(UserProfileStatus.Active);
        profileA.EmailVerified.ShouldBeTrue();
        profileB.Status.ShouldBe(UserProfileStatus.Deactivated);
        profileB.EmailVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task Redelivery_Is_Idempotent()
    {
        var store = new FakeUserProfileStore();
        var profile = UserProfileEntity.Create("identity-id", "user@example.com", "user", DateTime.UtcNow);
        await store.CreateAsync(profile, CancellationToken.None);

        var handler = new UserIdentityVerifiedEventHandler(store);
        var eventModel = new UserIdentityVerifiedEvent("identity-id", "user@example.com", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, CancellationToken.None);
        await handler.HandleAsync(eventModel, CancellationToken.None);

        profile.Status.ShouldBe(UserProfileStatus.Active);
        profile.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task Unknown_UserIdentityId_Is_NoOp()
    {
        var store = new FakeUserProfileStore();
        var handler = new UserIdentityVerifiedEventHandler(store);
        var eventModel = new UserIdentityVerifiedEvent("missing-id", "user@example.com", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, CancellationToken.None);

        store.Profiles.ShouldBeEmpty();
    }

    sealed class FakeUserProfileStore : IUserProfileStore
    {
        public List<UserProfileEntity> Profiles { get; } = [];

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct)
            => Task.FromResult(Profiles.Any(p => p.NormalizedEmail == normalizedEmail));

        public Task<UserProfileEntity?> FindByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
            => Task.FromResult(Profiles.SingleOrDefault(p => p.UserIdentityId == userIdentityId));

        public Task CreateAsync(UserProfileEntity profile, CancellationToken ct)
        {
            Profiles.Add(profile);

            return Task.CompletedTask;
        }

        public Task ActivateByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
        {
            var profile = Profiles.SingleOrDefault(p => p.UserIdentityId == userIdentityId);

            if (profile is null)
                return Task.CompletedTask;

            profile.Status = UserProfileStatus.Active;
            profile.EmailVerified = true;

            return Task.CompletedTask;
        }

        public Task UpdateDisplayNameAsync(string userIdentityId, string displayName, CancellationToken ct)
        {
            var profile = Profiles.Single(p => p.UserIdentityId == userIdentityId);
            profile.DisplayName = displayName;

            return Task.CompletedTask;
        }

        public Task<bool> TryUpdatePictureObjectKeyAsync(string userIdentityId,
                                                              string? expectedPictureObjectKey,
                                                              string? pictureObjectKey,
                                                              CancellationToken ct)
        {
            var profile = Profiles.Single(p => p.UserIdentityId == userIdentityId);

            if (profile.PictureObjectKey != expectedPictureObjectKey)
                return Task.FromResult(false);

            profile.PictureObjectKey = pictureObjectKey;

            return Task.FromResult(true);
        }
    }
}