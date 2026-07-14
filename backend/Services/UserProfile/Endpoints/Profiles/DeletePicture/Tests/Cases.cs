using System.Net;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UserProfile.Tests;

namespace Endpoints.Profiles.DeletePicture.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Missing_Access_Token_Is_Rejected()
    {
        var (rsp, _) = await DeletePictureAsync(null, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_Profile_Is_Not_Found()
    {
        var token = App.CreateAccessToken("missing-identity-id");

        var (rsp, _) = await DeletePictureAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound, AuthHeader(rsp));
    }

    [Fact]
    public async Task Inactive_Profile_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(status: UserProfileStatus.Deactivated, ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, _) = await DeletePictureAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden, AuthHeader(rsp));
    }

    [Fact]
    public async Task Delete_Without_Picture_Is_Idempotent()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, res) = await DeletePictureAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(rsp));
        res.ShouldNotBeNull();
        res.PictureUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Indeterminate_Clear_Still_Deletes_Committed_Picture()
    {
        var profile = UserProfileEntity.Create("identity-id", "jane@example.com", "Jane", DateTime.UtcNow);
        profile.PictureObjectKey = "old.jpg";
        var store = new IndeterminateClearStore(profile);

        var cleared = await Endpoint.TryClearPictureKeyAsync(store, profile.UserIdentityId, "old.jpg", Cancellation);

        cleared.ShouldBeTrue();
        profile.PictureObjectKey.ShouldBeNull();
    }

    [Fact]
    public async Task Storage_Rejects_Sibling_Path_With_Shared_Root_Prefix()
    {
        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;
        var absoluteRoot = Path.GetFullPath(Path.IsPathRooted(settings.StorageRoot)
                                                ? settings.StorageRoot
                                                : Path.Combine(Directory.GetCurrentDirectory(), settings.StorageRoot));
        var outsideRoot = $"{absoluteRoot}-outside-{Guid.NewGuid():N}";
        var outsideFile = Path.Combine(outsideRoot, "keep.txt");
        Directory.CreateDirectory(outsideRoot);
        await File.WriteAllTextAsync(outsideFile, "keep", Cancellation);
        var objectKey = Path.GetRelativePath(absoluteRoot, outsideFile).Replace(Path.DirectorySeparatorChar, '/');
        var storage = App.Services.GetRequiredService<IProfilePictureStorage>();

        try
        {
            var exception = await Record.ExceptionAsync(() => storage.DeleteAsync(objectKey, Cancellation));

            exception.ShouldBeOfType<InvalidOperationException>();
            File.Exists(outsideFile).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(outsideRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Storage_Rejects_Symbolic_Links_Beneath_Root()
    {
        if (OperatingSystem.IsWindows())
            return;

        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;
        var absoluteRoot = Path.GetFullPath(Path.IsPathRooted(settings.StorageRoot)
                                                ? settings.StorageRoot
                                                : Path.Combine(Directory.GetCurrentDirectory(), settings.StorageRoot));
        var linkPath = Path.Combine(absoluteRoot, $"link-{Guid.NewGuid():N}");
        var outsideRoot = Path.Combine(Path.GetTempPath(), $"profile-pictures-outside-{Guid.NewGuid():N}");
        var outsideFile = Path.Combine(outsideRoot, "keep.txt");
        Directory.CreateDirectory(absoluteRoot);
        Directory.CreateDirectory(outsideRoot);
        await File.WriteAllTextAsync(outsideFile, "keep", Cancellation);
        Directory.CreateSymbolicLink(linkPath, outsideRoot);
        var objectKey = $"{Path.GetFileName(linkPath)}/keep.txt";
        var storage = App.Services.GetRequiredService<IProfilePictureStorage>();

        try
        {
            var exception = await Record.ExceptionAsync(() => storage.DeleteAsync(objectKey, Cancellation));

            exception.ShouldBeOfType<InvalidOperationException>();
            File.Exists(outsideFile).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(linkPath);
            Directory.Delete(outsideRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Active_Profile_Picture_Is_Deleted()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;
        var absoluteRoot = Path.IsPathRooted(settings.StorageRoot)
                               ? settings.StorageRoot
                               : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), settings.StorageRoot));

        var uploadRsp = await UploadPictureAsync(token, await CreateJpegBytesAsync(), Cancellation);
        uploadRsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(uploadRsp));

        var storedBefore = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        storedBefore.ShouldNotBeNull();
        storedBefore.PictureObjectKey.ShouldNotBeNullOrWhiteSpace();
        var filePath = Path.Combine(absoluteRoot, storedBefore.PictureObjectKey!.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(filePath).ShouldBeTrue();

        var (rsp, res) = await DeletePictureAsync(token, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(rsp));
        res.ShouldNotBeNull();
        res.Id.ShouldBe(profile.ID);
        res.PictureUrl.ShouldBeNull();

        var storedAfter = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        storedAfter.ShouldNotBeNull();
        storedAfter.PictureObjectKey.ShouldBeNull();
        File.Exists(filePath).ShouldBeFalse();
    }

    static string AuthHeader(HttpResponseMessage rsp)
        => string.Join(" | ", rsp.Headers.WwwAuthenticate.Select(h => h.ToString()));

    static async Task<byte[]> CreateJpegBytesAsync()
    {
        using var image = new Image<Rgba32>(320, 240, Color.SteelBlue);
        await using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);

        return ms.ToArray();
    }

    async Task<HttpResponseMessage> UploadPictureAsync(string accessToken, byte[] bytes, CancellationToken ct)
    {
        using var client = App.CreateClient(o => o.DefaultRequestHeaders.Authorization = new("Bearer", accessToken));

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new("image/jpeg");
        content.Add(fileContent, "File", "avatar.jpg");

        return await client.PutAsync("/profiles/me/picture", content, ct);
    }

    async Task<(HttpResponseMessage rsp, Response? res)> DeletePictureAsync(string? accessToken, CancellationToken ct)
    {
        using var client = App.CreateClient(
            o =>
            {
                if (accessToken is not null)
                    o.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            });

        var rsp = await client.DeleteAsync("/profiles/me/picture", ct);
        Response? res = null;

        if (rsp.IsSuccessStatusCode)
            res = await rsp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);

        return (rsp, res);
    }

    sealed class IndeterminateClearStore(UserProfileEntity profile) : IUserProfileStore
    {
        public Task<bool> TryUpdatePictureObjectKeyAsync(string userIdentityId,
                                                         string? expectedPictureObjectKey,
                                                         string? pictureObjectKey,
                                                         CancellationToken ct)
        {
            profile.PictureObjectKey = pictureObjectKey;
            throw new TimeoutException("Result unknown after write.");
        }

        public Task<UserProfileEntity?> FindByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
            => Task.FromResult<UserProfileEntity?>(profile);

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct) => throw new NotSupportedException();
        public Task CreateAsync(UserProfileEntity value, CancellationToken ct) => throw new NotSupportedException();
        public Task ActivateByUserIdentityIdAsync(string userIdentityId, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateDisplayNameAsync(string userIdentityId, string displayName, CancellationToken ct) => throw new NotSupportedException();
    }
}