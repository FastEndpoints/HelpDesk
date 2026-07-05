namespace UserProfile.Tests;

public class Sut : AppFixture<Program>
{
    protected override void ConfigureApp(IWebHostBuilder app)
    {
        app.UseContentRoot(Directory.GetCurrentDirectory());
        app.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RegisterTestEventReceivers();
    }

    protected override async ValueTask OnCachedWafDisposedAsync()
    {
        await DB.Default.DropCollectionAsync<UserProfileEntity>();
    }
}
