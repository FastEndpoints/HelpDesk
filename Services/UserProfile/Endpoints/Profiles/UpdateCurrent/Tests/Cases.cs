using System.Net;
using UserProfile.Tests;

namespace Endpoints.Profiles.UpdateCurrent.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Missing_Access_Token_Is_Rejected()
    {
        var (rsp, _) = await UpdateCurrentProfileAsync(null, new() { DisplayName = "New Name" }, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_User_Input()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, _) = await UpdateCurrentProfileAsync(token, new() { DisplayName = "" }, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_Profile_Is_Not_Found()
    {
        var token = App.CreateAccessToken("missing-identity-id");

        var (rsp, _) = await UpdateCurrentProfileAsync(token, new() { DisplayName = "New Name" }, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound, AuthHeader(rsp));
    }

    [Fact]
    public async Task Inactive_Profile_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(status: UserProfileStatus.Deactivated, ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, _) = await UpdateCurrentProfileAsync(token, new() { DisplayName = "New Name" }, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden, AuthHeader(rsp));
    }

    [Fact]
    public async Task Active_Profile_DisplayName_Is_Updated()
    {
        var profile = await App.CreateProfileAsync(displayName: "Jane User", ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, res) = await UpdateCurrentProfileAsync(token, new() { DisplayName = "  Jane Updated  " }, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(rsp));
        res.ShouldNotBeNull();
        res.Id.ShouldBe(profile.ID);
        res.Email.ShouldBe(profile.Email);
        res.DisplayName.ShouldBe("Jane Updated");
        res.Status.ShouldBe(UserProfileStatus.Active.ToString());

        var stored = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        stored.ShouldNotBeNull();
        stored.DisplayName.ShouldBe("Jane Updated");
    }

    static string AuthHeader(HttpResponseMessage rsp)
        => string.Join(" | ", rsp.Headers.WwwAuthenticate.Select(h => h.ToString()));

    async Task<TestResult<Response>> UpdateCurrentProfileAsync(string? accessToken, Request request, CancellationToken ct)
    {
        using var client = App.CreateClient(
            o =>
            {
                if (accessToken is not null)
                    o.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            });

        return await client.PUTAsync<Endpoint, Request, Response>(request);
    }
}
