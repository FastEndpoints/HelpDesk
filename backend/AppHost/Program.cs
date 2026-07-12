var builder = DistributedApplication.CreateBuilder(args);

var mongodbUserName = builder.AddParameter("mongodb-username", "helpdesk", publishValueAsDefault: true);
var mongodbPassword = builder.AddParameter("mongodb-password", "helpdesk-local-password", publishValueAsDefault: true);
var mongodb = builder
    .AddMongoDB("mongodb", 27017, mongodbUserName, mongodbPassword)
    .WithImageTag("7.0.30");

var identity = builder
    .AddProject<Projects.Services_UserIdentity>(
        "identity",
        options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(env: "UserIdentity__HttpPort")
    .WithReference(mongodb, "MongoDB")
    .WaitFor(mongodb);

var profile = builder
    .AddProject<Projects.Services_UserProfile>(
        "profile",
        options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(env: "UserProfile__HttpPort")
    .WithReference(mongodb, "MongoDB")
    .WaitFor(mongodb)
    .WaitFor(identity);

builder
    .AddProject<Projects.Services_Notifications>(
        "notifications",
        options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(mongodb, "MongoDB")
    .WaitFor(mongodb)
    .WaitFor(identity);

builder
    .AddViteApp("frontend", "../../frontend")
    .WithPnpm(install: false)
    .WithEnvironment("IDENTITY_API_BASE_URL", identity.GetEndpoint("http"))
    .WithEnvironment("PROFILE_API_BASE_URL", profile.GetEndpoint("http"))
    .WaitFor(identity)
    .WaitFor(profile);

builder.Build().Run();
