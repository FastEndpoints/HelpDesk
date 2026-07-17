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
    readonly object _gate = new();
    readonly List<EmailMessage> _sent = [];
    TaskCompletionSource _signal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyCollection<EmailMessage> Sent
    {
        get
        {
            lock (_gate)
                return _sent.ToArray();
        }
    }

    public Task SendEmailAsync(EmailMessage message, CancellationToken ct)
    {
        lock (_gate)
        {
            _sent.Add(message);
            _signal.TrySetResult();
            _signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        return Task.CompletedTask;
    }

    public Task<EmailMessage> WaitForEmailAsync(CancellationToken ct)
        => WaitForEmailAsync(_ => true, ct);

    public async Task<EmailMessage> WaitForEmailAsync(Func<EmailMessage, bool> match, CancellationToken ct)
    {
        while (true)
        {
            Task wait;

            lock (_gate)
            {
                var index = _sent.FindIndex(m => match(m));

                if (index >= 0)
                {
                    var email = _sent[index];
                    _sent.RemoveAt(index);

                    return email;
                }

                wait = _signal.Task;
            }

            await wait.WaitAsync(ct);
        }
    }
}