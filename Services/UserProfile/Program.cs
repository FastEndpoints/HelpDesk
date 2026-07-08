using Common.StorageProvider;
using Contracts.UserIdentity;
using Contracts.UserProfile;
using MongoDB.Driver;
using Scalar.AspNetCore;
using Subscriptions.UserIdentity.Registration;
using Subscriptions.UserIdentity.Verification;
using NotificationService = Contracts.Notifications.Service;
using UserIdentityService = Contracts.UserIdentity.Service;
using UserProfileService = Contracts.UserProfile.Service;

#if DEBUG
using Xunit.Runner.InProc.SystemConsole;

if (args.Contains("@@"))
    return await ConsoleRunner.Run(args);
#endif

var bld = WebApplication.CreateBuilder(args);

if (bld.Environment.IsEnvironment("Testing"))
    bld.Configuration.AddUserSecrets<Program>(optional: true);

var settings = bld.Configuration.Get<UserProfileSettings>() ?? new();
bld.Services.Configure<UserProfileSettings>(bld.Configuration);

bld.WebHost.ConfigureKestrel(
    o =>
    {
        o.ListenInterProcess(UserProfileService.Name);    // grpc transport
        o.ListenLocalhost(settings.UserProfile.HttpPort); // http endpoints
    });

bld.Services
   .AddAuthenticationJwtBearer(
       o =>
       {
           o.SigningKey = settings.UserProfile.Jwt.PublicKey;
           o.SigningStyle = TokenSigningStyle.Asymmetric;
       },
       b =>
       {
           b.TokenValidationParameters.ValidIssuer = settings.UserProfile.Jwt.Issuer;
           b.TokenValidationParameters.ValidAudience = settings.UserProfile.Jwt.Audience;
       })
   .AddAuthorization();

bld.Services
   .AddFastEndpoints()
   .AddEventSubscriberStorageProvider<EventRecord, EventStorageProvider>()
   .AddSingleton<IUserProfileStore, MongoUserProfileStore>()
   .AddHandlerServer();

if (!bld.Environment.IsProduction())
{
    bld.Services.OpenApiDocument(
        o =>
        {
            o.DocumentName = "v1";
            o.Version = "v1";
        });
}

var app = bld.Build();

var db = await DB.InitAsync(settings.UserProfile.DatabaseName, MongoClientSettings.FromConnectionString(settings.ConnectionStrings.MongoDB));
await UserProfileDatabase.InitializeAsync(db);

app.UseAuthentication()
   .UseAuthorization()
   .UseFastEndpoints(c => c.Errors.UseProblemDetails());

app.MapHandlers<EventRecord, EventStorageProvider>(h => h.RegisterEventHub<UserProfileRegisteredEvent>([NotificationService.Name]));

app.MapRemote(
    UserIdentityService.Name,
    c =>
    {
        c.SubscribeWithExplicitId<UserIdentityRegisteredEvent, UserIdentityRegisteredEventHandler>(UserProfileService.Name);
        c.SubscribeWithExplicitId<UserIdentityVerifiedEvent, UserIdentityVerifiedEventHandler>(UserProfileService.Name);
    });

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();

return 0;

[HttpGet("/")]
sealed class DummyEndpoint : EndpointWithoutRequest
{
    public override Task HandleAsync(CancellationToken c)
        => Send.OkAsync();
}