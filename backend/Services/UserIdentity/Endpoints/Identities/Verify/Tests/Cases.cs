using System.Net;
using Contracts.UserIdentity;
using UserIdentity.Tests;

namespace Identities.Verify.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Unknown_Verification_Code_Is_Rejected()
    {
        var (rsp, _) = await App.Client.GETAsync<Endpoint, Request, ProblemDetails>(new()
        {
            VerificationCode = "missing-code"
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Locked_Account_Is_Not_Verified()
    {
        var identity = await App.CreateIdentityAsync(
            $"identity-{Guid.NewGuid():N}@example.com",
            status: UserIdentityStatus.Locked,
            ct: Cancellation);

        var (rsp, _) = await App.Client.GETAsync<Endpoint, Request, ProblemDetails>(new()
        {
            VerificationCode = identity.VerificationCode
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var stored = await App.FindByEmailAsync(identity.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.Status.ShouldBe(UserIdentityStatus.Locked);
    }

    [Fact]
    public async Task Deactivated_Account_Is_Activated()
    {
        var identity = await App.CreateIdentityAsync(
            $"identity-{Guid.NewGuid():N}@example.com",
            status: UserIdentityStatus.Deactivated,
            ct: Cancellation);

        var (rsp, res) = await App.Client.GETAsync<Endpoint, Request, string>(new()
        {
            VerificationCode = identity.VerificationCode
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe("Account verified.");

        var stored = await App.FindByEmailAsync(identity.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.Status.ShouldBe(UserIdentityStatus.Active);

        var published = (await App.Services
                                  .GetTestEventReceiver<UserIdentityVerifiedEvent>()
                                  .WaitForMatchAsync(e => e.UserIdentityId == identity.ID, ct: Cancellation))
            .Single();

        published.Email.ShouldBe(identity.Email);
        published.VerifiedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Active_Account_Verification_Is_Idempotent()
    {
        var identity = await App.CreateIdentityAsync($"identity-{Guid.NewGuid():N}@example.com", ct: Cancellation);

        var (rsp, res) = await App.Client.GETAsync<Endpoint, Request, string>(new()
        {
            VerificationCode = identity.VerificationCode
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe("Account verified.");
    }
}
