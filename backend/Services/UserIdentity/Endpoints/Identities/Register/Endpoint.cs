using Contracts.UserIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Identities.Register;

sealed class Endpoint(
    IUserIdentityStore store,
    IPasswordHasher<UserIdentityEntity> hasher,
    IOptions<UserIdentitySettings> settings)
    : Endpoint<Request, string>
{
    public override void Configure()
    {
        Post("identities/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var frontendBaseUrl = settings.Value.UserIdentity.FrontendBaseUrl?.Trim();

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            ThrowError("Frontend base URL is not configured.");

        var passwordHash = hasher.HashPassword(null!, r.Password);
        var identity = UserIdentityEntity.Create(r.Email, passwordHash, DateTime.UtcNow);

        try
        {
            await store.CreateAsync(identity, ct);
        }
        catch (DuplicateIdentityEmailException)
        {
            ThrowError(x => x.Email, "Email address is in use!");
        }

        var baseUrl = frontendBaseUrl.TrimEnd('/');

        new UserIdentityRegisteredEvent(identity.ID, identity.Email, identity.CreatedAt)
            .Broadcast();

        new UserIdentityVerificationIssuedEvent(identity.ID, identity.Email, identity.VerificationCode, baseUrl, identity.VerificationIssuedAt)
            .Broadcast();

        await Send.OkAsync("Signup successful. Please check your email for a verification link.", ct);
    }
}