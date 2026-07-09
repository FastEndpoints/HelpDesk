namespace Contracts.UserIdentity;

public static class EventSubscribers
{
    // == Contracts.UserProfile.Service.Name
    public static readonly string[] UserIdentityRegistered = ["USER_PROFILE_SERVICE"];

    // == Contracts.Notifications.Service.Name
    public static readonly string[] UserIdentityVerificationIssued = ["NOTIFICATIONS_SERVICE"];

    // == Contracts.UserProfile.Service.Name
    public static readonly string[] UserIdentityVerified = ["USER_PROFILE_SERVICE"];
}
