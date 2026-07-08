using Contracts.UserProfile;
using MongoDB.Driver;
using Subscriptions.UserProfile.Registration;
using NotificationService = Contracts.Notifications.Service;
using UserProfileService = Contracts.UserProfile.Service;

#if DEBUG
using Xunit.Runner.InProc.SystemConsole;

if (args.Contains("@@"))
    return await ConsoleRunner.Run(args);
#endif

var bld = WebApplication.CreateBuilder(args);

if (bld.Environment.IsEnvironment("Testing"))
    bld.Configuration.AddUserSecrets<Program>(optional: true);

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

app.UseFastEndpoints(c => c.Errors.UseProblemDetails());

app.UseJobQueues(
    o =>
    {
        o.LimitsFor<SendEmailCommand>(maxConcurrency: 1, timeLimit: TimeSpan.FromMinutes(2));
    });

app.MapRemote(
    UserProfileService.Name,
    c =>
    {
        c.SubscribeWithExplicitId<UserProfileRegisteredEvent, UserProfileRegisteredEventHandler>(NotificationService.Name);
    });

await app.RunAsync();

return 0;

[HttpGet("/")]
sealed class DummyEndpoint : EndpointWithoutRequest
{
    public override Task HandleAsync(CancellationToken c)
        => Task.CompletedTask;
}