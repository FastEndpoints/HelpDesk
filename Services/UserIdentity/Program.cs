using Common.StorageProvider;
using Contracts.UserIdentity;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using Scalar.AspNetCore;

#if DEBUG
using Xunit.Runner.InProc.SystemConsole;

if (args.Contains("@@"))
    return await ConsoleRunner.Run(args);
#endif

var bld = WebApplication.CreateBuilder(args);

if (bld.Environment.IsEnvironment("Testing"))
    bld.Configuration.AddUserSecrets<Program>(optional: true);

bld.WebHost.ConfigureKestrel(
    o =>
    {
        o.ListenInterProcess(Service.Name);                                           // grpc transport
        o.ListenLocalhost(bld.Configuration.GetValue("UserIdentity:HttpPort", 5000)); // http endpoints
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

var db = await DB.InitAsync(
             bld.Configuration.GetValue<string>("UserIdentity:DatabaseName") ?? "HelpDesk_UserIdentity",
             MongoClientSettings.FromConnectionString(bld.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017"));
await UserIdentityDatabase.InitializeAsync(db);

app.UseFastEndpoints(c => c.Errors.UseProblemDetails());
app.MapHandlers<EventRecord, EventStorageProvider>(
    h =>
    {
        h.RegisterEventHub<UserIdentityRegisteredEvent>();
        h.RegisterEventHub<UserIdentityVerifiedEvent>();
    });

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();

return 0;