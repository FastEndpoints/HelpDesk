using Contracts.UserIdentity;
using Notifications.Tests;

namespace Subscriptions.UserIdentity.VerificationIssued.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Sends_Welcome_Email_From_UserIdentityVerificationIssuedEvent()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityVerificationIssuedEventHandler();
        var eventModel = new UserIdentityVerificationIssuedEvent(
            "identity-id",
            "user@example.com",
            "verification code/with symbols?",
            "https://helpdesk.test/",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(Cancellation);
        email.ToEmail.ShouldBe(eventModel.Email);
        email.ToName.ShouldBe("user");
        email.Subject.ShouldBe("Welcome to HelpDesk");
        email.HtmlTemplate.ShouldBe(EmailTemplate.Welcome);
        email.MergeFields["DisplayName"].ShouldBe("user");
        email.MergeFields["VerificationLink"].ShouldBe(
            "https://helpdesk.test/verify/verification%20code%2Fwith%20symbols%3F");
    }

    [Fact]
    public async Task Builds_Verification_Link_When_BaseUrl_Has_No_Trailing_Slash()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityVerificationIssuedEventHandler();
        var eventModel = new UserIdentityVerificationIssuedEvent(
            "identity-id",
            "user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(Cancellation);
        email.MergeFields["VerificationLink"].ShouldBe("https://helpdesk.test/verify/abc123");
    }
}
