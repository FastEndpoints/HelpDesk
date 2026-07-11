using Common.Tools;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Endpoints.Identities.Login;

sealed class Endpoint(IUserIdentityStore store, IPasswordHasher<UserIdentityEntity> hasher, IOptions<UserIdentitySettings> options) : Endpoint<Request, Response>
{
    readonly UserIdentitySettings.JwtSettings _jwtSettings = options.Value.UserIdentity.Jwt;

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

        var expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.AccessTokenDays);
        var groups = identity.Groups is { Length: > 0 } ? identity.Groups : PermissionGroups.Defaults;
        var accessToken = JwtBearer.CreateToken(
            o =>
            {
                o.SigningKey = _jwtSettings.PrivateKeyPem;
                o.SigningStyle = TokenSigningStyle.Asymmetric;
                o.SigningAlgorithm = SecurityAlgorithms.RsaSha256;
                o.Issuer = _jwtSettings.Issuer;
                o.Audience = _jwtSettings.Audience;
                o.ExpireAt = expiresAt;
                o.User["sub"] = identity.ID;
                o.User.Roles.AddRange(groups);
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