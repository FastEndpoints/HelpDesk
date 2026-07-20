using Contracts.UserIdentity;
using Notifications.Tests;

namespace Subscriptions.UserIdentity.VerificationIssued.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Sends_Welcome_Email_From_UserIdentityVerificationIssuedEvent()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityVerificationIssuedEventHandler(new EmptyDisplayNameStore());
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
    public async Task Uses_Projected_DisplayName_When_Available()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var store = new FakeDisplayNameStore { Names = { ["identity-id-welcome-projected"] = "Preferred Name" } };
        var handler = new UserIdentityVerificationIssuedEventHandler(store);
        var eventModel = new UserIdentityVerificationIssuedEvent(
            "identity-id-welcome-projected",
            "welcome-user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(m => m.ToEmail == eventModel.Email, Cancellation);
        email.ToName.ShouldBe("Preferred Name");
        email.MergeFields["DisplayName"].ShouldBe("Preferred Name");
    }

    [Fact]
    public async Task Builds_Verification_Link_When_BaseUrl_Has_No_Trailing_Slash()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityVerificationIssuedEventHandler(new EmptyDisplayNameStore());
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
        var handler = new UserIdentityVerificationIssuedEventHandler(new EmptyDisplayNameStore());
        var eventModel = new UserIdentityVerificationIssuedEvent(
            "identity-id-idempotent",
            "idempotent-user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);
        var expectedKey = UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey(
            eventModel.UserIdentityId,
            eventModel.VerificationCode);

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
            IdempotencyKey = UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey("queue-return", "code-1"),
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

    [Fact]
    public async Task Different_Verification_Codes_Store_Separate_Jobs()
    {
        var handler = new UserIdentityVerificationIssuedEventHandler(new EmptyDisplayNameStore());
        var identityId = "identity-id-resend";
        var first = new UserIdentityVerificationIssuedEvent(
            identityId,
            "resend-user@example.com",
            "code-first",
            "https://helpdesk.test",
            DateTime.UtcNow);
        var second = first with { VerificationCode = "code-second", IssuedAt = DateTime.UtcNow };

        await handler.HandleAsync(first, Cancellation);
        await handler.HandleAsync(second, Cancellation);

        var firstKey = UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey(identityId, "code-first");
        var secondKey = UserIdentityVerificationIssuedEventHandler.JobIdempotencyKey(identityId, "code-second");

        var jobs = await DB.Default
                           .Find<JobRecord>()
                           .Match(r => r.IdempotencyKey == firstKey || r.IdempotencyKey == secondKey)
                           .ExecuteAsync(Cancellation);

        jobs.Count.ShouldBe(2);
        jobs.Select(j => j.IdempotencyKey).ShouldBe([firstKey, secondKey], ignoreOrder: true);
    }

    sealed class EmptyDisplayNameStore : IDisplayNameStore
    {
        public Task UpsertAsync(string userIdentityId, string displayName, DateTime updatedAt, CancellationToken ct)
            => Task.CompletedTask;

        public Task<string?> FindAsync(string userIdentityId, CancellationToken ct)
            => Task.FromResult<string?>(null);
    }

    sealed class FakeDisplayNameStore : IDisplayNameStore
    {
        public Dictionary<string, string> Names { get; } = new();

        public Task UpsertAsync(string userIdentityId, string displayName, DateTime updatedAt, CancellationToken ct)
        {
            Names[userIdentityId] = displayName;

            return Task.CompletedTask;
        }

        public Task<string?> FindAsync(string userIdentityId, CancellationToken ct)
            => Task.FromResult(Names.TryGetValue(userIdentityId, out var name) ? name : null);
    }
}
