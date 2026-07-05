using Contracts.UserIdentity;
using Contracts.UserProfile;

namespace Subscriptions.UserIdentity.Registration;

sealed class UserIdentityRegisteredEventHandler(IUserProfileStore profiles)
    : IEventHandler<UserIdentityRegisteredEvent>
{
    public async Task HandleAsync(UserIdentityRegisteredEvent eventModel, CancellationToken ct)
    {
        var profile = UserProfileEntity.Create(
            eventModel.Email,
            GetDefaultDisplayName(eventModel.Email),
            eventModel.RegisteredAt);

        try
        {
            await profiles.CreateAsync(profile, ct);
        }
        catch (DuplicateEmailException)
        {
            return;
        }

        new UserProfileRegisteredEvent(profile.ID, profile.Email, profile.DisplayName, eventModel.VerificationCode, eventModel.BaseUrl, profile.CreatedAt)
            .Broadcast();
    }

    static string GetDefaultDisplayName(string email)
    {
        var trimmedEmail = email.Trim();
        var atIndex = trimmedEmail.IndexOf('@');

        return atIndex > 0
                   ? trimmedEmail[..atIndex]
                   : trimmedEmail;
    }
}