using Contracts.UserIdentity;
using MongoDB.Driver;
using Services.Notifications;
using Subscriptions.UserIdentity.VerificationIssued;
using NotificationService = Contracts.Notifications.Service;
using UserIdentityService = Contracts.UserIdentity.Service;

#if DEBUG
using Xunit.Runner.InProc.SystemConsole;

if (args.Contains("@@"))
    return await ConsoleRunner.Run(args);
#endif

var bld = WebApplication.CreateBuilder(args);

var settings = bld.Configuration.Get<NotificationSettings>() ?? new();
bld.Services.Configure<NotificationSettings>(bld.Configuration);

bld.WebHost.ConfigureKestrel(o => o.ListenInterProcess(NotificationService.Name));

if (bld.Environment.IsProduction() && settings.Smtp.Enabled)
    bld.Services.AddSingleton<IEmailSender, SmtpService>();
else
    bld.Services.AddSingleton<IEmailSender, NullEmailSender>();

bld.Services
   .AddFastEndpoints()
   .AddEventSubscriberStorageProvider<EventRecord, EventStorageProvider>()
   .AddJobQueues<JobRecord, JobStorageProvider>()
   .AddHandlerServer();

var app = bld.Build();

var db = await DB.InitAsync(settings.Notifications.DatabaseName, MongoClientSettings.FromConnectionString(settings.ConnectionStrings.MongoDB));
await NotificationsDatabase.InitializeAsync(db);

app.UseFastEndpoints(
    c =>
    {
        c.Binding.ReflectionCache.AddFromServicesNotifications();
        c.Errors.UseProblemDetails();
    });

app.UseJobQueues(
    o =>
    {
        o.LimitsFor<SendEmailCommand>(maxConcurrency: 1, timeLimit: TimeSpan.FromMinutes(2));
    });

app.MapRemote(
    UserIdentityService.Name,
    c =>
    {
        c.SubscriberID = NotificationService.Name;
        c.Subscribe<UserIdentityVerificationIssuedEvent, UserIdentityVerificationIssuedEventHandler>();
    });

await app.RunAsync();

return 0;

[HttpGet("/")]
sealed class DummyEndpoint : EndpointWithoutRequest
{
    public override Task HandleAsync(CancellationToken c)
        => Task.CompletedTask;
}
