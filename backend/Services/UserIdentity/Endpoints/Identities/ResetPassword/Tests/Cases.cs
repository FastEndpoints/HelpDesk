using System.Net;
using UserIdentity.Tests;
using LoginEndpoint = Endpoints.Identities.Login.Endpoint;
using LoginRequest = Endpoints.Identities.Login.Request;
using LoginResponse = Endpoints.Identities.Login.Response;

namespace Identities.ResetPassword.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    const string NewPassword = "new correct horse battery";

    static Request NewRequest(string resetCode, string? password = null)
        => new()
        {
            ResetCode = resetCode,
            Password = password ?? NewPassword
        };

    [Fact]
    public async Task Invalid_Password_Returns_Bad_Request()
    {
        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(
            NewRequest("some-code", "short"));

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Empty_Password_Returns_Bad_Request()
    {
        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(
            NewRequest("some-code", ""));

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Password_Over_Max_Length_Returns_Bad_Request()
    {
        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(
            NewRequest("some-code", new string('x', 129)));

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_Code_Is_Rejected()
    {
        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(
            NewRequest("deadbeef"));

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Detail.ShouldBe(Endpoint.InvalidOrExpiredMessage);
    }

    [Fact]
    public async Task Expired_Token_Is_Rejected_And_Removed()
    {
        var email = $"identity-{Guid.NewGuid():N}@example.com";
        var identity = await App.CreateIdentityAsync(email, ct: Cancellation);
        var (_, rawCode) = await App.CreatePasswordResetTokenAsync(
            identity,
            createdAt: DateTime.UtcNow.AddHours(-2),
            expireAt: DateTime.UtcNow.AddMinutes(-1),
            ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(
            NewRequest(rawCode));

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Detail.ShouldBe(Endpoint.InvalidOrExpiredMessage);
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(0);

        var (loginRsp, _) = await App.Client.POSTAsync<LoginEndpoint, LoginRequest, ProblemDetails>(
            new LoginRequest
            {
                Email = email,
                Password = NewPassword
            });

        loginRsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Valid_Token_Updates_Password_And_Deletes_Token()
    {
        var email = $"identity-{Guid.NewGuid():N}@example.com";
        var identity = await App.CreateIdentityAsync(email, ct: Cancellation);
        var (_, rawCode) = await App.CreatePasswordResetTokenAsync(identity, ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(NewRequest(rawCode));

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(0);

        var (loginRsp, loginRes) = await App.Client.POSTAsync<LoginEndpoint, LoginRequest, LoginResponse>(
            new LoginRequest
            {
                Email = email,
                Password = NewPassword
            });

        loginRsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        loginRes.Id.ShouldBe(identity.ID);

        var (oldLoginRsp, _) = await App.Client.POSTAsync<LoginEndpoint, LoginRequest, ProblemDetails>(
            new LoginRequest
            {
                Email = email,
                Password = Sut.ValidPassword
            });

        oldLoginRsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Code_Cannot_Be_Reused()
    {
        var email = $"identity-{Guid.NewGuid():N}@example.com";
        var identity = await App.CreateIdentityAsync(email, ct: Cancellation);
        var (_, rawCode) = await App.CreatePasswordResetTokenAsync(identity, ct: Cancellation);

        var (firstRsp, _) = await App.Client.POSTAsync<Endpoint, Request, string>(NewRequest(rawCode));
        firstRsp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var (secondRsp, secondRes) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(
            NewRequest(rawCode, "another horse battery staple"));

        secondRsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        secondRes.Detail.ShouldBe(Endpoint.InvalidOrExpiredMessage);
    }

    [Fact]
    public async Task Token_For_Non_Active_Identity_Is_Rejected()
    {
        var email = $"identity-{Guid.NewGuid():N}@example.com";
        var identity = await App.CreateIdentityAsync(
            email,
            status: UserIdentityStatus.Deactivated,
            ct: Cancellation);
        var (_, rawCode) = await App.CreatePasswordResetTokenAsync(identity, ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(NewRequest(rawCode));

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Detail.ShouldBe(Endpoint.InvalidOrExpiredMessage);
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(0);
    }
}
