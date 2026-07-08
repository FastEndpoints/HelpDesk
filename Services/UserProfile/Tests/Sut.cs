using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace UserProfile.Tests;

public class Sut : AppFixture<Program>
{
    internal string CreateAccessToken(string userIdentityId)
    {
        var jwtSettings = Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.Jwt;

        return JwtBearer.CreateToken(
            o =>
            {
                o.SigningKey = jwtSettings.PrivateKey;
                o.SigningStyle = TokenSigningStyle.Asymmetric;
                o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
                o.Issuer = jwtSettings.Issuer;
                o.Audience = jwtSettings.Audience;
                o.ExpireAt = DateTime.UtcNow.AddHours(1);
                o.User["sub"] = userIdentityId;
            });
    }

    internal async Task<UserProfileEntity> CreateProfileAsync(string? userIdentityId = null,
                                                              string? email = null,
                                                              string? displayName = null,
                                                              UserProfileStatus status = UserProfileStatus.Active,
                                                              bool emailVerified = true,
                                                              CancellationToken ct = default)
    {
        var profile = UserProfileEntity.Create(
            userIdentityId ?? $"identity-{Guid.NewGuid():N}",
            email ?? $"profile-{Guid.NewGuid():N}@example.com",
            displayName ?? "Test User",
            DateTime.UtcNow);

        profile.Status = status;
        profile.EmailVerified = emailVerified;

        await DB.Default.InsertAsync(profile, ct);

        return profile;
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
        await DB.Default.DropCollectionAsync<UserProfileEntity>();
    }
}