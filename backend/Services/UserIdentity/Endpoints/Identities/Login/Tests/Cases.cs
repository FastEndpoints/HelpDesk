using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Common.Tools;
using Identities;
using UserIdentity.Tests;

namespace Endpoints.Identities.Login.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    static Request NewRequest(string? email = null, string? password = null)
        => new()
        {
            Email = email ?? $"identity-{Guid.NewGuid():N}@example.com",
            Password = password ?? Sut.ValidPassword
        };

    [Fact]
    public async Task Invalid_User_Input()
    {
        var req = new Request
        {
            Email = "not-an-email",
            Password = ""
        };

        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_Email_Is_Rejected()
    {
        var req = NewRequest();

        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_Password_Is_Rejected()
    {
        var req = NewRequest(password: "wrong password");
        await App.CreateIdentityAsync(req.Email, ct: Cancellation);

        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Inactive_Account_Is_Rejected()
    {
        var req = NewRequest();
        await App.CreateIdentityAsync(req.Email, status: UserIdentityStatus.Locked, ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Detail.ShouldBe("Account not verified.");
        rsp.Headers.TryGetValues(VerificationResend.AvailableInHeaderName, out var values).ShouldBeTrue();
        int.Parse(values!.Single()).ShouldBeInRange(0, (int)VerificationResend.Cooldown.TotalSeconds);
    }

    [Fact]
    public async Task Deactivated_Account_Returns_Remaining_Resend_Cooldown_Header()
    {
        var req = NewRequest();
        var issuedAt = DateTime.UtcNow.AddMinutes(-10);
        await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            verificationIssuedAt: issuedAt,
            ct: Cancellation);

        var before = DateTime.UtcNow;
        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);
        var after = DateTime.UtcNow;

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Detail.ShouldBe("Account not verified.");
        rsp.Headers.TryGetValues(VerificationResend.AvailableInHeaderName, out var values).ShouldBeTrue();

        var seconds = int.Parse(values!.Single());
        var minExpected = VerificationResend.AvailableInSeconds(issuedAt, after);
        var maxExpected = VerificationResend.AvailableInSeconds(issuedAt, before);
        seconds.ShouldBeInRange(minExpected, maxExpected);
        seconds.ShouldBeGreaterThan(0);
        seconds.ShouldBeLessThan((int)VerificationResend.Cooldown.TotalSeconds);
    }

    [Fact]
    public async Task Deactivated_Account_Past_Cooldown_Returns_Zero_Resend_Header()
    {
        var req = NewRequest();
        await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            verificationIssuedAt: DateTime.UtcNow - VerificationResend.Cooldown - TimeSpan.FromMinutes(1),
            ct: Cancellation);

        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        rsp.Headers.TryGetValues(VerificationResend.AvailableInHeaderName, out var values).ShouldBeTrue();
        int.Parse(values!.Single()).ShouldBe(0);
    }

    [Fact]
    public async Task Successful_Login_Returns_Access_Token()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(req.Email, ct: Cancellation);
        var beforeLogin = DateTime.UtcNow;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, Response>(new()
        {
            Email = $" {req.Email.ToUpperInvariant()} ",
            Password = req.Password
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Id.ShouldBe(identity.ID);
        res.Email.ShouldBe(identity.Email);
        res.AccessToken.ShouldNotBeNullOrWhiteSpace();
        res.ExpiresAt.ShouldBeGreaterThan(beforeLogin.AddDays(6));
        res.ExpiresAt.ShouldBeLessThan(DateTime.UtcNow.AddDays(8));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(res.AccessToken);
        jwt.Claims.ShouldContain(c => c.Type == "role" && c.Value == PermissionGroups.User);
        jwt.Claims.ShouldContain(c => c.Type == "sub" && c.Value == identity.ID);
    }
}
