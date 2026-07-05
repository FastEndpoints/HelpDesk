using Contracts.UserProfile;
using Notifications.Tests;

namespace Subscriptions.UserProfile.Registration.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Sends_Welcome_Email_From_UserProfileRegisteredEvent()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserProfileRegisteredEventHandler();
        var eventModel = new UserProfileRegisteredEvent(
            "profile-id",
            "user@example.com",
            "Jane User",
            "verification code/with symbols?",
            "https://helpdesk.test/",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(Cancellation);
        email.ToEmail.ShouldBe(eventModel.Email);
        email.ToName.ShouldBe(eventModel.DisplayName);
        email.Subject.ShouldBe("Welcome to HelpDesk");
        email.HtmlTemplate.ShouldBe(EmailTemplate.Welcome);
        email.MergeFields["DisplayName"].ShouldBe(eventModel.DisplayName);
        email.MergeFields["VerificationLink"].ShouldBe(
            "https://helpdesk.test/identities/verify/verification%20code%2Fwith%20symbols%3F");
    }

    [Fact]
    public async Task Builds_Verification_Link_When_BaseUrl_Has_No_Trailing_Slash()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserProfileRegisteredEventHandler();
        var eventModel = new UserProfileRegisteredEvent(
            "profile-id",
            "user@example.com",
            "Jane User",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(Cancellation);
        email.MergeFields["VerificationLink"].ShouldBe("https://helpdesk.test/identities/verify/abc123");
    }
}
