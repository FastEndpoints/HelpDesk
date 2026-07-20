using Contracts.UserIdentity;
using Contracts.UserProfile;
using MongoDB.Driver;
using Services.Notifications;
using Subscriptions.UserIdentity.PasswordResetIssued;
using Subscriptions.UserIdentity.VerificationIssued;
using Subscriptions.UserProfile.DisplayNameUpdated;
using Subscriptions.UserProfile.Registration;
using NotificationService = Contracts.Notifications.Service;
using UserIdentityService = Contracts.UserIdentity.Service;
using UserProfileService = Contracts.UserProfile.Service;

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
   .AddSingleton<IDisplayNameStore, MongoDisplayNameStore>()
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
        o.IdempotencyKeyFor<SendEmailCommand>(c => c.IdempotencyKey);
    });

app.MapRemote(
    UserIdentityService.Name,
    c =>
    {
        c.SubscriberID = NotificationService.Name;
        c.Subscribe<UserIdentityVerificationIssuedEvent, UserIdentityVerificationIssuedEventHandler>();
        c.Subscribe<UserIdentityPasswordResetIssuedEvent, UserIdentityPasswordResetIssuedEventHandler>();
    });

app.MapRemote(
    UserProfileService.Name,
    c =>
    {
        c.SubscriberID = NotificationService.Name;
        c.Subscribe<UserProfileRegisteredEvent, UserProfileRegisteredEventHandler>();
        c.Subscribe<UserProfileDisplayNameUpdatedEvent, UserProfileDisplayNameUpdatedEventHandler>();
    });

await app.RunAsync();

return 0;

[HttpGet("/")]
sealed class DummyEndpoint : EndpointWithoutRequest
{
    public override Task HandleAsync(CancellationToken c)
        => Task.CompletedTask;
}
