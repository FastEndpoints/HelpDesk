using Contracts.UserIdentity;
using Contracts.UserProfile;
using UserProfile.Tests;

namespace Subscriptions.UserIdentity.Registration.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Creates_Profile_From_UserIdentityRegisteredEvent()
    {
        var store = new FakeUserProfileStore();
        var handler = new UserIdentityRegisteredEventHandler(store);
        var registeredAt = DateTime.UtcNow;
        var eventModel = new UserIdentityRegisteredEvent("identity-id", "user@example.com", registeredAt);

        await handler.HandleAsync(eventModel, Cancellation);

        var profile = store.Created.Single();
        profile.UserIdentityId.ShouldBe(eventModel.UserIdentityId);
        profile.Email.ShouldBe(eventModel.Email);
        profile.NormalizedEmail.ShouldBe(eventModel.Email.ToUpperInvariant());
        profile.DisplayName.ShouldBe("user");
        profile.CreatedAt.ShouldBe(registeredAt);
    }

    [Fact]
    public async Task Uses_Trimmed_Email_Local_Part_As_Default_DisplayName()
    {
        var store = new FakeUserProfileStore();
        var handler = new UserIdentityRegisteredEventHandler(store);
        var eventModel = new UserIdentityRegisteredEvent("identity-id", "  Display.Name@example.com  ", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var profile = store.Created.Single();
        profile.Email.ShouldBe("Display.Name@example.com");
        profile.DisplayName.ShouldBe("Display.Name");
    }

    [Fact]
    public async Task Duplicate_Email_Is_Ignored()
    {
        var store = new FakeUserProfileStore { ThrowDuplicateEmail = true };
        var handler = new UserIdentityRegisteredEventHandler(store);
        var eventModel = new UserIdentityRegisteredEvent("identity-id", "user@example.com", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        store.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Broadcasts_UserProfileRegisteredEvent_After_Profile_Creation()
    {
        var store = new FakeUserProfileStore();
        var handler = new UserIdentityRegisteredEventHandler(store);
        var eventModel = new UserIdentityRegisteredEvent("identity-id", $"user-{Guid.NewGuid():N}@example.com", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var profile = store.Created.Single();
        var published = (await App.Services
                                  .GetTestEventReceiver<UserProfileRegisteredEvent>()
                                  .WaitForMatchAsync(e => e.Email == profile.Email, ct: Cancellation))
            .Single();

        published.UserProfileId.ShouldBe(profile.ID);
        published.UserIdentityId.ShouldBe(profile.UserIdentityId);
        published.Email.ShouldBe(profile.Email);
        published.DisplayName.ShouldBe(profile.DisplayName);
        published.RegisteredAt.ShouldBe(profile.CreatedAt);
    }

    sealed class FakeUserProfileStore : IUserProfileStore
    {
        public List<UserProfileEntity> Created { get; } = [];
        public bool ThrowDuplicateEmail { get; init; }

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct)
            => Task.FromResult(Created.Any(p => p.NormalizedEmail == normalizedEmail));

        public Task<UserProfileEntity?> FindByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
            => Task.FromResult(Created.SingleOrDefault(p => p.UserIdentityId == userIdentityId));

        public Task CreateAsync(UserProfileEntity profile, CancellationToken ct)
        {
            if (ThrowDuplicateEmail)
                throw new DuplicateEmailException(profile.NormalizedEmail);

            Created.Add(profile);

            return Task.CompletedTask;
        }

        public Task ActivateByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateDisplayNameAsync(string userIdentityId, string displayName, CancellationToken ct)
        {
            var profile = Created.Single(p => p.UserIdentityId == userIdentityId);
            profile.DisplayName = displayName;

            return Task.CompletedTask;
        }

        public Task<bool> TryUpdatePictureObjectKeyAsync(string userIdentityId,
                                                              string? expectedPictureObjectKey,
                                                              string? pictureObjectKey,
                                                              CancellationToken ct)
        {
            var profile = Created.Single(p => p.UserIdentityId == userIdentityId);

            if (profile.PictureObjectKey != expectedPictureObjectKey)
                return Task.FromResult(false);

            profile.PictureObjectKey = pictureObjectKey;

            return Task.FromResult(true);
        }
    }
}
