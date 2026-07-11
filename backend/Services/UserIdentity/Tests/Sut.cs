using Common.Tools;
using Microsoft.AspNetCore.Identity;

namespace UserIdentity.Tests;

public class Sut : AppFixture<Program>
{
    internal const string ValidPassword = "correct horse battery staple";

    internal async Task<UserIdentityEntity?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = email.NormalizeForLookup();

        var identities = await DB.Default
                                 .Find<UserIdentityEntity>()
                                 .Match(i => i.NormalizedEmail == normalizedEmail)
                                 .Limit(1)
                                 .ExecuteAsync(ct);

        return identities.SingleOrDefault();
    }

    internal async Task<UserIdentityEntity> CreateIdentityAsync(
        string email,
        string password = ValidPassword,
        UserIdentityStatus status = UserIdentityStatus.Active,
        CancellationToken ct = default)
    {
        var passwordHash = Services
            .GetRequiredService<IPasswordHasher<UserIdentityEntity>>()
            .HashPassword(null!, password);

        var identity = new UserIdentityEntity
        {
            Email = email.Trim(),
            NormalizedEmail = email.NormalizeForLookup(),
            PasswordHash = passwordHash,
            VerificationCode = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            Groups = [.. PermissionGroups.Defaults],
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await DB.Default.InsertAsync(identity, ct);

        return identity;
    }

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
        await DB.Default.DropCollectionAsync<UserIdentityEntity>();
    }
}
