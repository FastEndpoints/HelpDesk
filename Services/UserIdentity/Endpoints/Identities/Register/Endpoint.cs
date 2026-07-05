using Contracts.UserIdentity;
using Microsoft.AspNetCore.Identity;

namespace Identities.Register;

sealed class Endpoint(IUserIdentityStore store, IPasswordHasher<UserIdentityEntity> hasher)
    : Endpoint<Request, string>
{
    public override void Configure()
    {
        Post("identities/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
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

        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');

        new UserIdentityRegisteredEvent(identity.ID, identity.Email, identity.VerificationCode, baseUrl, identity.CreatedAt)
            .Broadcast();

        await Send.OkAsync("Signup successful. Please check your email for a verification link.", ct);
    }
}