using Common.StorageProvider;
using Contracts.UserIdentity;
using UserProfileService = Contracts.UserProfile.Service;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using Scalar.AspNetCore;
using Services.UserIdentity;

#if DEBUG
using Xunit.Runner.InProc.SystemConsole;

if (args.Contains("@@"))
    return await ConsoleRunner.Run(args);
#endif

var bld = WebApplication.CreateBuilder(args);

if (bld.Environment.IsEnvironment("Testing"))
    bld.Configuration.AddUserSecrets<Program>(optional: true);

var settings = bld.Configuration.Get<UserIdentitySettings>() ?? new();
bld.Services.Configure<UserIdentitySettings>(bld.Configuration);

bld.WebHost.ConfigureKestrel(
    o =>
    {
        o.ListenInterProcess(Service.Name);                // grpc transport
        o.ListenLocalhost(settings.UserIdentity.HttpPort); // http endpoints
    });

bld.Services
   .AddFastEndpoints()
   .AddEventSubscriberStorageProvider<EventRecord, EventStorageProvider>()
   .AddSingleton<IUserIdentityStore, MongoUserIdentityStore>()
   .AddSingleton<IPasswordHasher<UserIdentityEntity>, PasswordHasher<UserIdentityEntity>>()
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

var db = await DB.InitAsync(settings.UserIdentity.DatabaseName, MongoClientSettings.FromConnectionString(settings.ConnectionStrings.MongoDB));
await UserIdentityDatabase.InitializeAsync(db);

app.UseFastEndpoints(
    c =>
    {
        c.Binding.ReflectionCache.AddFromServicesUserIdentity();
        c.Errors.UseProblemDetails();
    });

app.MapHandlers<EventRecord, EventStorageProvider>(
    h =>
    {
        h.RegisterEventHub<UserIdentityRegisteredEvent>([UserProfileService.Name]);
        h.RegisterEventHub<UserIdentityVerifiedEvent>([UserProfileService.Name]);
    });

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();

return 0;