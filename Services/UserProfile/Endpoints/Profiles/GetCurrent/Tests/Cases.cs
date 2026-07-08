using System.Net;
using UserProfile.Tests;

namespace Endpoints.Profiles.GetCurrent.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Missing_Access_Token_Is_Rejected()
    {
        var (rsp, _) = await GetCurrentProfileAsync(null, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_Profile_Is_Not_Found()
    {
        var token = App.CreateAccessToken("missing-identity-id");

        var (rsp, _) = await GetCurrentProfileAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound, AuthHeader(rsp));
    }

    [Fact]
    public async Task Inactive_Profile_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(status: UserProfileStatus.Deactivated, ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, _) = await GetCurrentProfileAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden, AuthHeader(rsp));
    }

    [Fact]
    public async Task Active_Profile_Is_Returned()
    {
        var profile = await App.CreateProfileAsync(displayName: "Jane User", ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, res) = await GetCurrentProfileAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(rsp));
        res.ShouldNotBeNull();
        res.Id.ShouldBe(profile.ID);
        res.Email.ShouldBe(profile.Email);
        res.DisplayName.ShouldBe(profile.DisplayName);
        res.Status.ShouldBe(UserProfileStatus.Active.ToString());
    }

    static string AuthHeader(HttpResponseMessage rsp)
        => string.Join(" | ", rsp.Headers.WwwAuthenticate.Select(h => h.ToString()));

    async Task<TestResult<Response>> GetCurrentProfileAsync(string? accessToken, CancellationToken ct)
    {
        using var client = App.CreateClient(
            o =>
            {
                if (accessToken is not null)
                    o.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            });

        return await client.GETAsync<Endpoint, Response>();
    }
}