using System.Net;
using Contracts.UserIdentity;
using UserIdentity.Tests;

namespace Identities.Register.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    static Request NewRequest()
        => new()
        {
            Email = $"identity-{Guid.NewGuid():N}@example.com",
            Password = Sut.ValidPassword
        };

    [Fact]
    public async Task Invalid_User_Input()
    {
        var req = new Request
        {
            Email = "not-an-email",
            Password = "short"
        };

        var (rsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Successful_UserIdentity_Registration()
    {
        var req = NewRequest();

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe("Signup successful. Please check your email for a verification link.");

        var stored = await App.FindByEmailAsync(req.Email, Cancellation);
        stored.ShouldNotBeNull();
        stored.ID.ShouldNotBeEmpty();
        stored.Email.ShouldBe(req.Email);
        stored.NormalizedEmail.ShouldBe(req.Email.ToUpperInvariant());
        stored.PasswordHash.ShouldNotBeEmpty();
        stored.PasswordHash.ShouldNotBe(req.Password);
        stored.VerificationCode.ShouldNotBeNullOrWhiteSpace();
        stored.Status.ShouldBe(UserIdentityStatus.Deactivated);

        var published = (await App.Services
                                  .GetTestEventReceiver<UserIdentityRegisteredEvent>()
                                  .WaitForMatchAsync(e => e.UserIdentityId == stored.ID, ct: Cancellation))
            .Single();

        published.Email.ShouldBe(stored.Email);
        published.VerificationCode.ShouldBe(stored.VerificationCode);
        published.BaseUrl.ShouldBe(App.Client.BaseAddress!.ToString().TrimEnd('/'));
        published.RegisteredAt.ShouldBe(stored.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Duplicate_Email_Is_Rejected()
    {
        var req = NewRequest();
        var (firstRsp, firstRes) = await App.Client.POSTAsync<Endpoint, Request, string>(req);
        firstRsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstRes.ShouldBe("Signup successful. Please check your email for a verification link.");

        var firstIdentity = await App.FindByEmailAsync(req.Email, Cancellation);
        firstIdentity.ShouldNotBeNull();

        var dupe = new Request
        {
            Email = $" {req.Email.ToUpperInvariant()} ",
            Password = Sut.ValidPassword
        };

        var (dupeRsp, _) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(dupe);

        dupeRsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityRegisteredEvent>()
                                 .WaitForMatchAsync(e => e.UserIdentityId == firstIdentity.ID, ct: Cancellation);

        published.Count().ShouldBe(1);
    }
}