using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notifications.Tests;

public sealed class Sut : AppFixture<Program>
{
    protected override void ConfigureApp(IWebHostBuilder app)
    {
        app.UseContentRoot(Directory.GetCurrentDirectory());
        app.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IEmailSender>();
        services.AddSingleton<TestEmailSender>();
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<TestEmailSender>());
    }

    protected override async ValueTask OnCachedWafDisposedAsync()
    {
        await DB.Default.DropCollectionAsync<JobRecord>();
        await DB.Default.DropCollectionAsync<EventRecord>();
    }
}

public sealed class TestEmailSender : IEmailSender
{
    readonly Queue<EmailMessage> _sent = [];
    TaskCompletionSource<EmailMessage> _nextEmail = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyCollection<EmailMessage> Sent => _sent;

    public Task SendEmailAsync(EmailMessage message, CancellationToken ct)
    {
        _sent.Enqueue(message);
        _nextEmail.TrySetResult(message);

        return Task.CompletedTask;
    }

    public async Task<EmailMessage> WaitForEmailAsync(CancellationToken ct)
    {
        if (_sent.TryDequeue(out var existing))
            return existing;

        await _nextEmail.Task.WaitAsync(ct);
        _nextEmail = new(TaskCreationOptions.RunContinuationsAsynchronously);

        return _sent.Dequeue();
    }
}