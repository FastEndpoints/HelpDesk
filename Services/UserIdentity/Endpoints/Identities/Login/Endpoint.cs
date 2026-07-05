using Common.Tools;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Endpoints.Identities.Login;

sealed class Endpoint(IUserIdentityStore store, IPasswordHasher<UserIdentityEntity> hasher) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("identities/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var identity = await store.FindByEmailAsync(r.Email.NormalizeForLookup(), ct);

        if (identity is null)
            ThrowError("Invalid email or password.");

        var verification = hasher.VerifyHashedPassword(identity, identity.PasswordHash, r.Password);

        if (verification == PasswordVerificationResult.Failed)
            ThrowError("Invalid email or password.");

        if (identity.Status != UserIdentityStatus.Active)
            ThrowError("Account not verified.");

        var expiresAt = DateTime.UtcNow.AddDays(Config.GetValue("UserIdentity:Jwt:AccessTokenDays", 7));
        var accessToken = JwtBearer.CreateToken(
            o =>
            {
                o.SigningKey = Config.GetValue("UserIdentity:Jwt:PrivateKeyPem", "");
                o.SigningStyle = TokenSigningStyle.Asymmetric;
                o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
                o.Issuer = Config.GetValue("UserIdentity:Jwt:Issuer", "HelpDesk.UserIdentity");
                o.Audience = Config.GetValue("UserIdentity:Jwt:Audience", "HelpDesk.Services");
                o.ExpireAt = expiresAt;
                o.User["sub"] = identity.ID;
            });

        await Send.OkAsync(
            new()
            {
                Id = identity.ID,
                Email = identity.Email,
                AccessToken = accessToken,
                ExpiresAt = expiresAt
            },
            ct);
    }
}