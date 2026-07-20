using Contracts.UserIdentity;
using Notifications.Tests;

namespace Subscriptions.UserIdentity.PasswordResetIssued.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Sends_Password_Reset_Email_From_Event()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityPasswordResetIssuedEventHandler(new EmptyDisplayNameStore());
        var eventModel = new UserIdentityPasswordResetIssuedEvent(
            "identity-id-password-reset",
            "reset-user@example.com",
            "reset code/with symbols?",
            "https://helpdesk.test/",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(m => m.ToEmail == eventModel.Email, Cancellation);
        email.ToEmail.ShouldBe(eventModel.Email);
        email.ToName.ShouldBe("reset-user");
        email.Subject.ShouldBe("Reset your HelpDesk password");
        email.HtmlTemplate.ShouldBe(EmailTemplate.PasswordReset);
        email.MergeFields["DisplayName"].ShouldBe("reset-user");
        email.MergeFields["ResetLink"].ShouldBe(
            "https://helpdesk.test/reset-password/reset%20code%2Fwith%20symbols%3F");
    }

    [Fact]
    public async Task Uses_Projected_DisplayName_When_Available()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var store = new FakeDisplayNameStore { Names = { ["identity-id-reset-projected"] = "Jane Updated" } };
        var handler = new UserIdentityPasswordResetIssuedEventHandler(store);
        var eventModel = new UserIdentityPasswordResetIssuedEvent(
            "identity-id-reset-projected",
            "reset-user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(m => m.ToEmail == eventModel.Email, Cancellation);
        email.ToName.ShouldBe("Jane Updated");
        email.MergeFields["DisplayName"].ShouldBe("Jane Updated");
    }

    [Fact]
    public async Task Builds_Reset_Link_When_BaseUrl_Has_No_Trailing_Slash()
    {
        var sender = App.Services.GetRequiredService<TestEmailSender>();
        var handler = new UserIdentityPasswordResetIssuedEventHandler(new EmptyDisplayNameStore());
        var eventModel = new UserIdentityPasswordResetIssuedEvent(
            "identity-id-no-trailing-slash-reset",
            "slash-user@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);

        await handler.HandleAsync(eventModel, Cancellation);

        var email = await sender.WaitForEmailAsync(m => m.ToEmail == eventModel.Email, Cancellation);
        email.MergeFields["ResetLink"].ShouldBe("https://helpdesk.test/reset-password/abc123");
    }

    [Fact]
    public async Task Duplicate_Password_Reset_Issued_Event_Stores_Single_Job()
    {
        var handler = new UserIdentityPasswordResetIssuedEventHandler(new EmptyDisplayNameStore());
        var eventModel = new UserIdentityPasswordResetIssuedEvent(
            "identity-id-reset-idempotent",
            "idempotent-reset@example.com",
            "abc123",
            "https://helpdesk.test",
            DateTime.UtcNow);
        var expectedKey = UserIdentityPasswordResetIssuedEventHandler.JobIdempotencyKey(
            eventModel.UserIdentityId,
            eventModel.ResetCode);

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
    public async Task Different_Reset_Codes_Store_Separate_Jobs()
    {
        var handler = new UserIdentityPasswordResetIssuedEventHandler(new EmptyDisplayNameStore());
        var identityId = "identity-id-reset-resend";
        var first = new UserIdentityPasswordResetIssuedEvent(
            identityId,
            "resend-reset@example.com",
            "code-first",
            "https://helpdesk.test",
            DateTime.UtcNow);
        var second = first with { ResetCode = "code-second", IssuedAt = DateTime.UtcNow };

        await handler.HandleAsync(first, Cancellation);
        await handler.HandleAsync(second, Cancellation);

        var firstKey = UserIdentityPasswordResetIssuedEventHandler.JobIdempotencyKey(identityId, "code-first");
        var secondKey = UserIdentityPasswordResetIssuedEventHandler.JobIdempotencyKey(identityId, "code-second");

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
