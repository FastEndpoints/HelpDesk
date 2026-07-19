using System.Net;
using Contracts.UserIdentity;
using UserIdentity.Tests;

namespace Identities.ResendVerification.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    static Request NewRequest(string? email = null)
        => new()
        {
            Email = email ?? $"identity-{Guid.NewGuid():N}@example.com"
        };

    [Fact]
    public async Task Invalid_User_Input()
    {
        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(new()
        {
            Email = "not-an-email"
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Empty_Email_Returns_Bad_Request()
    {
        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(new()
        {
            Email = ""
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Email_Over_Max_Length_Returns_Bad_Request()
    {
        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(new()
        {
            Email = new string('a', 310) + "@example.com"
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_Email_Returns_Generic_Success()
    {
        var req = NewRequest();

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        (await App.FindByEmailAsync(req.Email, Cancellation)).ShouldBeNull();
    }

    [Fact]
    public async Task Active_Account_Returns_Generic_Success_Without_Reissue()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Active,
            verificationIssuedAt: DateTime.UtcNow.AddHours(-1),
            ct: Cancellation);
        var originalCode = identity.VerificationCode;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(new()
        {
            Email = $" {req.Email.ToUpperInvariant()} "
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        var stored = await App.FindByEmailAsync(req.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.VerificationCode.ShouldBe(originalCode);

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityVerificationIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID,
                                     timeoutSeconds: 1,
                                     ct: Cancellation);

        published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Deactivated_Account_Within_Cooldown_Returns_Generic_Success_Without_Reissue()
    {
        var req = NewRequest();
        var issuedAt = DateTime.UtcNow.AddMinutes(-5);
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            verificationIssuedAt: issuedAt,
            ct: Cancellation);
        var originalCode = identity.VerificationCode;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        var stored = await App.FindByEmailAsync(req.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.VerificationCode.ShouldBe(originalCode);
        stored.VerificationIssuedAt.ShouldBe(issuedAt, TimeSpan.FromMilliseconds(1));

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityVerificationIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID,
                                     timeoutSeconds: 1,
                                     ct: Cancellation);

        published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Deactivated_Account_After_Cooldown_Reissues_Verification()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            verificationIssuedAt: DateTime.UtcNow - Endpoint.ResendCooldown - TimeSpan.FromMinutes(1),
            ct: Cancellation);
        var originalCode = identity.VerificationCode;
        var beforeIssue = DateTime.UtcNow;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(new()
        {
            Email = $" {req.Email.ToUpperInvariant()} "
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        var stored = await App.FindByEmailAsync(req.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.Status.ShouldBe(UserIdentityStatus.Deactivated);
        stored.VerificationCode.ShouldNotBe(originalCode);
        stored.VerificationCode.ShouldNotBeNullOrWhiteSpace();
        stored.VerificationIssuedAt.ShouldBeGreaterThanOrEqualTo(beforeIssue.AddSeconds(-1));
        stored.VerificationIssuedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));

        var published = (await App.Services
                                  .GetTestEventReceiver<UserIdentityVerificationIssuedEvent>()
                                  .WaitForMatchAsync(e => e.UserIdentityId == identity.ID, ct: Cancellation))
            .Single();

        published.Email.ShouldBe(identity.Email);
        published.VerificationCode.ShouldBe(stored.VerificationCode);
        published.BaseUrl.ShouldBe("https://frontend.test");
        published.IssuedAt.ShouldBe(stored.VerificationIssuedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Deactivated_Account_At_Exact_Cooldown_Boundary_Reissues_Verification()
    {
        var req = NewRequest();
        // Slightly past equality wall-clock noise; gate is >= ResendCooldown.
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            verificationIssuedAt: DateTime.UtcNow - Endpoint.ResendCooldown,
            ct: Cancellation);
        var originalCode = identity.VerificationCode;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        var stored = await App.FindByEmailAsync(req.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.VerificationCode.ShouldNotBe(originalCode);

        var published = (await App.Services
                                  .GetTestEventReceiver<UserIdentityVerificationIssuedEvent>()
                                  .WaitForMatchAsync(e => e.UserIdentityId == identity.ID, ct: Cancellation))
            .Single();

        published.VerificationCode.ShouldBe(stored.VerificationCode);
    }

    [Fact]
    public async Task Second_Resend_Within_New_Cooldown_Does_Not_Reissue()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            verificationIssuedAt: DateTime.UtcNow - Endpoint.ResendCooldown - TimeSpan.FromMinutes(1),
            ct: Cancellation);

        var (firstRsp, firstRes) = await App.Client.POSTAsync<Endpoint, Request, string>(req);
        firstRsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstRes.ShouldBe(Endpoint.SuccessMessage);

        var afterFirst = await App.FindByEmailAsync(req.Email, Cancellation);
        afterFirst.ShouldNotBeNull();
        var codeAfterFirst = afterFirst.VerificationCode;
        var issuedAfterFirst = afterFirst.VerificationIssuedAt;

        var (secondRsp, secondRes) = await App.Client.POSTAsync<Endpoint, Request, string>(req);
        secondRsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondRes.ShouldBe(Endpoint.SuccessMessage);

        var afterSecond = await App.FindByEmailAsync(req.Email, Cancellation);
        afterSecond.ShouldNotBeNull();
        afterSecond.VerificationCode.ShouldBe(codeAfterFirst);
        afterSecond.VerificationIssuedAt.ShouldBe(issuedAfterFirst, TimeSpan.FromMilliseconds(1));

        // Only the first resend should have published for this identity.
        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityVerificationIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID &&
                                          e.VerificationCode == codeAfterFirst,
                                     ct: Cancellation);

        published.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Locked_Account_Returns_Generic_Success_Without_Reissue()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Locked,
            verificationIssuedAt: DateTime.UtcNow.AddHours(-1),
            ct: Cancellation);
        var originalCode = identity.VerificationCode;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        var stored = await App.FindByEmailAsync(req.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.VerificationCode.ShouldBe(originalCode);

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityVerificationIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID,
                                     timeoutSeconds: 1,
                                     ct: Cancellation);

        published.ShouldBeEmpty();
    }
}
