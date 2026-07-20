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
        DateTime? verificationIssuedAt = null,
        CancellationToken ct = default)
    {
        var passwordHash = Services
            .GetRequiredService<IPasswordHasher<UserIdentityEntity>>()
            .HashPassword(null!, password);

        var now = DateTime.UtcNow;
        var identity = new UserIdentityEntity
        {
            Email = email.Trim(),
            NormalizedEmail = email.NormalizeForLookup(),
            PasswordHash = passwordHash,
            VerificationCode = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            Groups = [.. PermissionGroups.Defaults],
            Status = status,
            CreatedAt = now,
            VerificationIssuedAt = verificationIssuedAt ?? now
        };

        await DB.Default.InsertAsync(identity, ct);

        return identity;
    }

    internal async Task<(PasswordResetTokenEntity Token, string RawCode)> CreatePasswordResetTokenAsync(
        UserIdentityEntity identity,
        DateTime? createdAt = null,
        DateTime? expireAt = null,
        CancellationToken ct = default)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var rawCode = PasswordResetTokenEntity.CreateRawCode();
        var token = new PasswordResetTokenEntity
        {
            UserIdentityId = identity.ID,
            NormalizedEmail = identity.NormalizedEmail,
            TokenHash = PasswordResetTokenEntity.HashCode(rawCode),
            CreatedAt = now,
            ExpireAt = expireAt ?? now.Add(Identities.PasswordReset.TokenLifetime)
        };

        await DB.Default.InsertAsync(token, ct);

        return (token, rawCode);
    }

    internal async Task<PasswordResetTokenEntity?> FindPasswordResetTokenByUserAsync(
        string userIdentityId,
        CancellationToken ct)
    {
        var tokens = await DB.Default
                             .Find<PasswordResetTokenEntity>()
                             .Match(t => t.UserIdentityId == userIdentityId)
                             .Sort(t => t.CreatedAt, MongoDB.Entities.Order.Descending)
                             .Limit(1)
                             .ExecuteAsync(ct);

        return tokens.SingleOrDefault();
    }

    internal async Task<int> CountPasswordResetTokensAsync(string userIdentityId, CancellationToken ct)
    {
        var tokens = await DB.Default
                             .Find<PasswordResetTokenEntity>()
                             .Match(t => t.UserIdentityId == userIdentityId)
                             .ExecuteAsync(ct);

        return tokens.Count;
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
        await DB.Default.DropCollectionAsync<PasswordResetTokenEntity>();
    }
}
