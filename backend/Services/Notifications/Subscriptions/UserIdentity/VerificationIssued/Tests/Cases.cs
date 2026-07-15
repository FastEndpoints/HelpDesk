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
            "identity-id-welcome-email",
            "welcome-user@example.com",
            "verification code/with symbols?",
            "https://helpdesk.test/",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(m => m.ToEmail == eventModel.Email, Cancellation);
        email.ToEmail.ShouldBe(eventModel.Email);
        email.ToName.ShouldBe("welcome-user");
        email.Subject.ShouldBe("Welcome to HelpDesk");
        email.HtmlTemplate.ShouldBe(EmailTemplate.Welcome);
        email.MergeFields["DisplayName"].ShouldBe("welcome-user");
        email.MergeFields["VerificationLink"].ShouldBe(
            "https://helpdesk.test/verify/verification%20code%2Fwith%20symbols%3F");
    }

    [Fact]
    public async Task Builds_Verification_Link_When_BaseUrl_Has_No_Trailing_Slash()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityVerificationIssuedEventHandler();
        var eventModel = new UserIdentityVerificationIssuedEvent(
            "identity-id-no-trailing-slash",
            "slash-user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(m => m.ToEmail == eventModel.Email, Cancellation);
        email.MergeFields["VerificationLink"].ShouldBe("https://helpdesk.test/verify/abc123");
    }

    [Fact]
    public async Task Duplicate_Verification_Issued_Event_Stores_Single_Job()
    {
        var handler = new UserIdentityVerificationIssuedEventHandler();
        var eventModel = new UserIdentityVerificationIssuedEvent(
            "identity-id-idempotent",
            "idempotent-user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);
        var expectedKey = UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey(eventModel.UserIdentityId);

        await handler.HandleAsync(eventModel, Cancellation);
        await handler.HandleAsync(eventModel, Cancellation);

        var jobs = await DB.Default
                           .Find<JobRecord>()
                           .Match(r => r.IdempotencyKey == expectedKey)
                           .ExecuteAsync(Cancellation);

        jobs.Count.ShouldBe(1);
        jobs[0].TrackingID.ShouldNotBe(Guid.Empty);
        jobs[0].IdempotencyKey.ShouldBe(expectedKey);
    }

    [Fact]
    public async Task QueueJobAsync_Returns_Existing_TrackingId_For_Duplicate_Key()
    {
        var command = new SendEmailCommand
        {
            IdempotencyKey = UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey("queue-return"),
            Message = new()
            {
                ToEmail = "queue-return@example.com",
                ToName = "user",
                Subject = "Welcome to HelpDesk",
                HtmlTemplate = EmailTemplate.Welcome
            }
        };

        var firstTrackingId = await command.QueueJobAsync(ct: Cancellation);
        var secondTrackingId = await command.QueueJobAsync(ct: Cancellation);

        firstTrackingId.ShouldNotBe(Guid.Empty);
        secondTrackingId.ShouldBe(firstTrackingId);

        var jobs = await DB.Default
                           .Find<JobRecord>()
                           .Match(r => r.IdempotencyKey == command.IdempotencyKey)
                           .ExecuteAsync(Cancellation);

        jobs.Count.ShouldBe(1);
        jobs[0].TrackingID.ShouldBe(firstTrackingId);
    }
}
