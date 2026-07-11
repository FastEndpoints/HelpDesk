using Contracts.UserIdentity;

namespace Identities.Verify;

sealed class Endpoint(IUserIdentityStore store) : Endpoint<Request, string>
{
    public override void Configure()
    {
        Get("identities/verify/{VerificationCode}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.VerificationCode))
            ThrowError("Invalid verification code.");

        var identity = await store.FindByVerificationCodeAsync(r.VerificationCode, ct);

        if (identity is null)
            ThrowError("Invalid verification code.");

        if (identity.Status == UserIdentityStatus.Active)
        {
            await Send.OkAsync("Account verified.", ct);
            return;
        }

        if (identity.Status != UserIdentityStatus.Deactivated)
            ThrowError("Account cannot be verified.");

        await store.ActivateAsync(identity.ID, ct);

        new UserIdentityVerifiedEvent(identity.ID, identity.Email, DateTime.UtcNow)
            .Broadcast();

        await Send.OkAsync("Account verified.", ct);
    }
}

sealed class Request
{
    public string VerificationCode { get; init; } = null!;
}
