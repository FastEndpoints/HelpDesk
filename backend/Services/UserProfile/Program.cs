using Auth;
using Common.StorageProvider;
using Contracts.UserIdentity;
using Contracts.UserProfile;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;
using Scalar.AspNetCore;
using Services.UserProfile;
using Subscriptions.UserIdentity.Registration;
using Subscriptions.UserIdentity.Verification;
using EventSubscribers = Contracts.UserProfile.EventSubscribers;
using UserIdentityService = Contracts.UserIdentity.Service;
using UserProfileService = Contracts.UserProfile.Service;

#if DEBUG
using Xunit.Runner.InProc.SystemConsole;

if (args.Contains("@@"))
    return await ConsoleRunner.Run(args);
#endif

var bld = WebApplication.CreateBuilder(args);

var settings = bld.Configuration.Get<UserProfileSettings>() ?? new();
var maxUploadBytes = settings.UserProfile.ProfilePictures.MaxUploadBytes;

if (maxUploadBytes <= 0 || maxUploadBytes > long.MaxValue - UserProfileSettings.ProfilePictureSettings.MultipartOverheadBytes)
    throw new InvalidOperationException("UserProfile:ProfilePictures:MaxUploadBytes must be a positive value with room for multipart framing.");

bld.Services.Configure<UserProfileSettings>(bld.Configuration);
bld.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxUploadBytes + UserProfileSettings.ProfilePictureSettings.MultipartOverheadBytes);

bld.WebHost.ConfigureKestrel(
    o =>
    {
        o.ListenInterProcess(UserProfileService.Name); // grpc transport

        if (bld.Environment.IsProduction())
            o.ListenAnyIP(settings.UserProfile.HttpPort); // private container HTTP endpoint
        else
            o.ListenLocalhost(settings.UserProfile.HttpPort); // local orchestrator HTTP endpoint
    });

bld.Services
   .AddAuthenticationJwtBearer(
       o =>
       {
           o.SigningKey = settings.UserProfile.Jwt.PublicKey;
           o.KeyIsPemEncoded = settings.UserProfile.Jwt.PublicKey.StartsWith("-----BEGIN", StringComparison.Ordinal);
           o.SigningStyle = TokenSigningStyle.Asymmetric;
       },
       b =>
       {
           b.TokenValidationParameters.ValidIssuer = settings.UserProfile.Jwt.Issuer;
           b.TokenValidationParameters.ValidAudience = settings.UserProfile.Jwt.Audience;
       })
   .AddAuthorization();

bld.Services
   .AddSingleton<IClaimsTransformation, PermissionClaimsTransformation>()
   .AddFastEndpoints()
   .AddEventSubscriberStorageProvider<EventRecord, EventStorageProvider>()
   .AddSingleton<IUserProfileStore, MongoUserProfileStore>()
   .AddSingleton<IProfilePictureStorage, LocalProfilePictureStorage>()
   .AddSingleton<IProfilePictureProcessor, ImageSharpProfilePictureProcessor>()
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

var pictureRoot = settings.UserProfile.ProfilePictures.StorageRoot;
var absolutePictureRoot = Path.IsPathRooted(pictureRoot)
                              ? pictureRoot
                              : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, pictureRoot));
Directory.CreateDirectory(absolutePictureRoot);

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(absolutePictureRoot),
        RequestPath = LocalProfilePictureStorage.RequestPath
    });

app.UseAuthentication()
   .UseAuthorization()
   .UseFastEndpoints(
       c =>
       {
           c.Binding.ReflectionCache.AddFromServicesUserProfile();
           c.Errors.UseProblemDetails();
       });

app.MapHandlers<EventRecord, EventStorageProvider>(
    h =>
    {
        h.RegisterEventHub<UserProfileRegisteredEvent>(EventSubscribers.UserProfileRegistered);
    });

app.MapRemote(
    UserIdentityService.Name,
    c =>
    {
        c.SubscriberID = UserProfileService.Name;
        c.Subscribe<UserIdentityRegisteredEvent, UserIdentityRegisteredEventHandler>();
        c.Subscribe<UserIdentityVerifiedEvent, UserIdentityVerifiedEventHandler>();
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