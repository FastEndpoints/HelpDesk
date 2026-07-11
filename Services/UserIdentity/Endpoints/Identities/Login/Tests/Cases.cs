using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Common.Tools;
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

        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
