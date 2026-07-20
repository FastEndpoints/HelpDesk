using System.Net;
using Contracts.UserIdentity;
using Microsoft.Extensions.Options;
using UserIdentity.Tests;

namespace Identities.ForgotPassword.Tests;

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
        rsp.Headers.TryGetValues(Identities.PasswordReset.AvailableInHeaderName, out var values).ShouldBeTrue();
        int.Parse(values!.Single()).ShouldBe((int)Identities.PasswordReset.RequestCooldown.TotalSeconds);

        (await App.FindByEmailAsync(req.Email, Cancellation)).ShouldBeNull();
    }

    [Fact]
    public async Task Deactivated_Account_Returns_Generic_Success_Without_Token()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Deactivated,
            ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);
        rsp.Headers.TryGetValues(Identities.PasswordReset.AvailableInHeaderName, out var deactivatedValues)
           .ShouldBeTrue();
        int.Parse(deactivatedValues!.Single())
           .ShouldBe((int)Identities.PasswordReset.RequestCooldown.TotalSeconds);
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(0);

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityPasswordResetIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID,
                                     timeoutSeconds: 1,
                                     ct: Cancellation);

        published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Locked_Account_Returns_Generic_Success_Without_Token()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(
            req.Email,
            status: UserIdentityStatus.Locked,
            ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);
        rsp.Headers.TryGetValues(Identities.PasswordReset.AvailableInHeaderName, out var lockedValues)
           .ShouldBeTrue();
        int.Parse(lockedValues!.Single())
           .ShouldBe((int)Identities.PasswordReset.RequestCooldown.TotalSeconds);
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(0);

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityPasswordResetIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID,
                                     timeoutSeconds: 1,
                                     ct: Cancellation);

        published.ShouldBeEmpty();
    }

    [Fact]
    public async Task Active_Account_Issues_Token_And_Event()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(req.Email, ct: Cancellation);
        var before = DateTime.UtcNow;

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(new()
        {
            Email = $" {req.Email.ToUpperInvariant()} "
        });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);
        rsp.Headers.TryGetValues(Identities.PasswordReset.AvailableInHeaderName, out var issuedValues)
           .ShouldBeTrue();
        int.Parse(issuedValues!.Single())
           .ShouldBe((int)Identities.PasswordReset.RequestCooldown.TotalSeconds);

        var stored = await App.FindPasswordResetTokenByUserAsync(identity.ID, Cancellation);
        stored.ShouldNotBeNull();
        stored.TokenHash.ShouldNotBeNullOrWhiteSpace();
        stored.NormalizedEmail.ShouldBe(identity.NormalizedEmail);
        stored.CreatedAt.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-1));
        stored.ExpireAt.ShouldBe(stored.CreatedAt.Add(Identities.PasswordReset.TokenLifetime), TimeSpan.FromSeconds(1));

        var published = (await App.Services
                                  .GetTestEventReceiver<UserIdentityPasswordResetIssuedEvent>()
                                  .WaitForMatchAsync(e => e.UserIdentityId == identity.ID, ct: Cancellation))
            .Single();

        published.Email.ShouldBe(identity.Email);
        published.BaseUrl.ShouldBe("https://frontend.test");
        published.ResetCode.ShouldNotBeNullOrWhiteSpace();
        published.ResetCode.ShouldNotBe(stored.TokenHash);
        PasswordResetTokenEntity.HashCode(published.ResetCode).ShouldBe(stored.TokenHash);
        published.IssuedAt.ShouldBe(stored.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Active_Account_Within_Cooldown_Does_Not_Reissue()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(req.Email, ct: Cancellation);

        var (firstRsp, firstRes) = await App.Client.POSTAsync<Endpoint, Request, string>(req);
        firstRsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstRes.ShouldBe(Endpoint.SuccessMessage);

        var afterFirst = await App.FindPasswordResetTokenByUserAsync(identity.ID, Cancellation);
        afterFirst.ShouldNotBeNull();
        var hashAfterFirst = afterFirst.TokenHash;

        var beforeSecond = DateTime.UtcNow;
        var (secondRsp, secondRes) = await App.Client.POSTAsync<Endpoint, Request, string>(req);
        var afterSecondCall = DateTime.UtcNow;
        secondRsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondRes.ShouldBe(Endpoint.SuccessMessage);
        secondRsp.Headers.TryGetValues(Identities.PasswordReset.AvailableInHeaderName, out var cooldownValues)
                 .ShouldBeTrue();

        var seconds = int.Parse(cooldownValues!.Single());
        var minExpected = Identities.PasswordReset.AvailableInSeconds(afterFirst!.CreatedAt, afterSecondCall);
        var maxExpected = Identities.PasswordReset.AvailableInSeconds(afterFirst.CreatedAt, beforeSecond);
        seconds.ShouldBeInRange(minExpected, maxExpected);
        seconds.ShouldBeGreaterThan(0);
        seconds.ShouldBeLessThanOrEqualTo((int)Identities.PasswordReset.RequestCooldown.TotalSeconds);

        var afterSecond = await App.FindPasswordResetTokenByUserAsync(identity.ID, Cancellation);
        afterSecond.ShouldNotBeNull();
        afterSecond.TokenHash.ShouldBe(hashAfterFirst);
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(1);

        var published = await App.Services
                                 .GetTestEventReceiver<UserIdentityPasswordResetIssuedEvent>()
                                 .WaitForMatchAsync(
                                     e => e.UserIdentityId == identity.ID,
                                     ct: Cancellation);

        published.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Active_Account_After_Cooldown_Reissues_And_Invalidates_Prior()
    {
        var req = NewRequest();
        var identity = await App.CreateIdentityAsync(req.Email, ct: Cancellation);
        var (_, oldRaw) = await App.CreatePasswordResetTokenAsync(
            identity,
            createdAt: DateTime.UtcNow - Identities.PasswordReset.RequestCooldown - TimeSpan.FromMinutes(1),
            ct: Cancellation);

        var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, string>(req);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldBe(Endpoint.SuccessMessage);

        var stored = await App.FindPasswordResetTokenByUserAsync(identity.ID, Cancellation);
        stored.ShouldNotBeNull();
        stored.TokenHash.ShouldNotBe(PasswordResetTokenEntity.HashCode(oldRaw));
        (await App.CountPasswordResetTokensAsync(identity.ID, Cancellation)).ShouldBe(1);

        var published = (await App.Services
                                  .GetTestEventReceiver<UserIdentityPasswordResetIssuedEvent>()
                                  .WaitForMatchAsync(e => e.UserIdentityId == identity.ID, ct: Cancellation))
            .Single();

        PasswordResetTokenEntity.HashCode(published.ResetCode).ShouldBe(stored.TokenHash);
    }

    [Fact]
    public async Task Missing_Frontend_Base_Url_Returns_Bad_Request()
    {
        var settings = App.Services.GetRequiredService<IOptions<UserIdentitySettings>>().Value.UserIdentity;
        var original = settings.FrontendBaseUrl;

        try
        {
            settings.FrontendBaseUrl = "   ";

            var (rsp, res) = await App.Client.POSTAsync<Endpoint, Request, ProblemDetails>(NewRequest());

            rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            res.Detail.ShouldBe("Frontend base URL is not configured.");
        }
        finally
        {
            settings.FrontendBaseUrl = original;
        }
    }
}
