using Common.Tools;
using Contracts.UserIdentity;

namespace Subscriptions.UserIdentity.Verification.Tests;

public class Cases
{
    [Fact]
    public async Task Activates_Profile_From_UserIdentityVerifiedEvent()
    {
        var store = new FakeUserProfileStore();
        var profile = UserProfileEntity.Create("user@example.com", "user", DateTime.UtcNow);
        await store.CreateAsync(profile, CancellationToken.None);

        var handler = new UserIdentityVerifiedEventHandler(store);
        var eventModel = new UserIdentityVerifiedEvent("identity-id", " USER@example.com ", DateTime.UtcNow);

        await handler.HandleAsync(eventModel, CancellationToken.None);

        profile.Status.ShouldBe(UserProfileStatus.Active);
        profile.EmailVerified.ShouldBeTrue();
    }

    sealed class FakeUserProfileStore : IUserProfileStore
    {
        readonly List<UserProfileEntity> profiles = [];

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct)
            => Task.FromResult(profiles.Any(p => p.NormalizedEmail == normalizedEmail));

        public Task CreateAsync(UserProfileEntity profile, CancellationToken ct)
        {
            profiles.Add(profile);
            return Task.CompletedTask;
        }

        public Task ActivateByEmailAsync(string email, CancellationToken ct)
        {
            var normalizedEmail = email.NormalizeForLookup();
            var profile = profiles.Single(p => p.NormalizedEmail == normalizedEmail);

            profile.Status = UserProfileStatus.Active;
            profile.EmailVerified = true;

            return Task.CompletedTask;
        }
    }
}
