using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UserProfile.Tests;

namespace Endpoints.Profiles.UploadPicture.Tests;

public class Cases(Sut App) : TestBase<Sut>
{
    [Fact]
    public async Task Missing_Access_Token_Is_Rejected()
    {
        var (rsp, _) = await UploadPictureAsync(null, await CreateJpegBytesAsync(400, 200), "avatar.jpg", "image/jpeg", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_Profile_Is_Not_Found()
    {
        var token = App.CreateAccessToken("missing-identity-id");

        var (rsp, _) = await UploadPictureAsync(token, await CreateJpegBytesAsync(400, 200), "avatar.jpg", "image/jpeg", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound, AuthHeader(rsp));
    }

    [Fact]
    public async Task Inactive_Profile_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(status: UserProfileStatus.Deactivated, ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, _) = await UploadPictureAsync(token, await CreateJpegBytesAsync(400, 200), "avatar.jpg", "image/jpeg", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden, AuthHeader(rsp));
    }

    [Fact]
    public async Task Invalid_File_Type_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);

        var (rsp, _) = await UploadPictureAsync(token, "not-an-image"u8.ToArray(), "notes.txt", "text/plain", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Corrupt_Image_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        byte[] corruptJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x00];

        var (rsp, _) = await UploadPictureAsync(token, corruptJpeg, "corrupt.jpg", "image/jpeg", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Renamed_Unsupported_Image_Format_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var gif = await CreateGifBytesAsync(100, 100);

        var (rsp, _) = await UploadPictureAsync(token, gif, "renamed.png", "image/png", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_File_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        using var client = App.CreateClient(o => o.DefaultRequestHeaders.Authorization = new("Bearer", token));
        using var content = new MultipartFormDataContent();

        var rsp = await client.PutAsync("/profiles/me/picture", content, Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Oversized_File_Is_Rejected()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var oversized = new byte[(5 * 1024 * 1024) + 1];

        var (rsp, _) = await UploadPictureAsync(token, oversized, "huge.jpg", "image/jpeg", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Configured_Upload_Limit_Is_Enforced()
    {
        var settings = new UserProfileSettings();
        settings.UserProfile.ProfilePictures.MaxUploadBytes = 4;
        var validator = new Request.Validator(Options.Create(settings));
        await using var stream = new MemoryStream(new byte[5]);
        var file = new FormFile(stream, 0, stream.Length, "File", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var request = new Request { File = file };
        var result = await validator.ValidateAsync(request, Cancellation);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Multipart_Limit_Includes_Configured_Upload_Size()
    {
        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;
        var formOptions = App.Services.GetRequiredService<IOptions<FormOptions>>().Value;

        formOptions.MultipartBodyLengthLimit.ShouldBe(settings.MaxUploadBytes + UserProfileSettings.ProfilePictureSettings.MultipartOverheadBytes);
    }

    [Fact]
    public async Task Excessive_Image_Dimensions_Are_Rejected()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var bytes = await CreatePngBytesAsync(ImageSharpProfilePictureProcessor.MaxInputDimensionPx + 1, 1);

        var (rsp, _) = await UploadPictureAsync(token, bytes, "huge-dimensions.png", "image/png", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Multi_Frame_Image_Is_Rejected()
    {
        var bytes = await CreateMultiFrameGifBytesAsync();
        await using var stream = new MemoryStream(bytes);
        var processor = App.Services.GetRequiredService<IProfilePictureProcessor>();

        var exception = await Record.ExceptionAsync(() => processor.ProcessAsync(stream, Cancellation));

        exception.ShouldBeOfType<InvalidImageFormatException>()
                 .Message.ShouldContain("frame count");
    }

    [Fact]
    public async Task Active_Profile_Picture_Is_Uploaded_And_Cropped()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;

        var (rsp, res) = await UploadPictureAsync(token, await CreateJpegBytesAsync(600, 300), "avatar.jpg", "image/jpeg", Cancellation);

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(rsp));
        res.ShouldNotBeNull();
        res.Id.ShouldBe(profile.ID);
        res.PictureUrl.ShouldNotBeNullOrWhiteSpace();
        res.PictureUrl.ShouldContain("/profile-pictures/profiles/");
        Uri.TryCreate(res.PictureUrl, UriKind.Absolute, out _).ShouldBeTrue();

        var stored = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        stored.ShouldNotBeNull();
        stored.PictureObjectKey.ShouldNotBeNullOrWhiteSpace();
        stored.PictureObjectKey.ShouldStartWith($"profiles/{profile.UserIdentityId}/");
        stored.PictureObjectKey.ShouldEndWith(".jpg");

        var absoluteRoot = Path.IsPathRooted(settings.StorageRoot)
                               ? settings.StorageRoot
                               : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), settings.StorageRoot));
        var filePath = Path.Combine(absoluteRoot, stored.PictureObjectKey.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(filePath).ShouldBeTrue();

        using var image = await Image.LoadAsync(filePath, Cancellation);
        image.Width.ShouldBe(300);
        image.Height.ShouldBe(300);
    }

    [Fact]
    public async Task Replacing_Picture_Deletes_Previous_File()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;
        var absoluteRoot = Path.IsPathRooted(settings.StorageRoot)
                               ? settings.StorageRoot
                               : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), settings.StorageRoot));

        var (firstRsp, firstRes) = await UploadPictureAsync(token, await CreatePngBytesAsync(400, 400), "one.png", "image/png", Cancellation);
        firstRsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(firstRsp));
        firstRes.ShouldNotBeNull();

        var firstStored = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        firstStored.ShouldNotBeNull();
        var firstKey = firstStored.PictureObjectKey!;
        var firstPath = Path.Combine(absoluteRoot, firstKey.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(firstPath).ShouldBeTrue();

        var (secondRsp, secondRes) = await UploadPictureAsync(token, await CreateJpegBytesAsync(500, 500), "two.jpg", "image/jpeg", Cancellation);
        secondRsp.StatusCode.ShouldBe(HttpStatusCode.OK, AuthHeader(secondRsp));
        secondRes.ShouldNotBeNull();
        secondRes.PictureUrl.ShouldNotBe(firstRes.PictureUrl);

        var secondStored = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        secondStored.ShouldNotBeNull();
        secondStored.PictureObjectKey.ShouldNotBe(firstKey);
        File.Exists(firstPath).ShouldBeFalse();
        File.Exists(Path.Combine(absoluteRoot, secondStored.PictureObjectKey!.Replace('/', Path.DirectorySeparatorChar))).ShouldBeTrue();
    }

    [Fact]
    public async Task Concurrent_Uploads_Do_Not_Leave_Orphaned_Files()
    {
        var profile = await App.CreateProfileAsync(ct: Cancellation);
        var token = App.CreateAccessToken(profile.UserIdentityId);
        var settings = App.Services.GetRequiredService<IOptions<UserProfileSettings>>().Value.UserProfile.ProfilePictures;
        var absoluteRoot = Path.IsPathRooted(settings.StorageRoot)
                               ? settings.StorageRoot
                               : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), settings.StorageRoot));
        var bytes = await CreateJpegBytesAsync(400, 400);

        var uploads = await Task.WhenAll(
                          Enumerable.Range(0, 5)
                                    .Select(i => UploadPictureAsync(token, bytes, $"avatar-{i}.jpg", "image/jpeg", Cancellation)));

        uploads.All(result => result.rsp.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict).ShouldBeTrue();

        var stored = await DB.Default.Find<UserProfileEntity>().OneAsync(profile.ID, Cancellation);
        stored.ShouldNotBeNull();
        stored.PictureObjectKey.ShouldNotBeNullOrWhiteSpace();
        var directory = Path.Combine(absoluteRoot, "profiles", profile.UserIdentityId);
        var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);

        files.Length.ShouldBe(1);
        files[0].ShouldBe(Path.Combine(absoluteRoot, stored.PictureObjectKey.Replace('/', Path.DirectorySeparatorChar)));
    }

    [Fact]
    public async Task Indeterminate_Update_Does_Not_Delete_Committed_File()
    {
        var profile = UserProfileEntity.Create("identity-id", "jane@example.com", "Jane", DateTime.UtcNow);
        var store = new IndeterminateUpdateStore(profile);
        var storage = new TrackingStorage();

        var updated = await Endpoint.TryCommitPictureKeyAsync(store, storage, profile.UserIdentityId, null, "new.jpg", Cancellation);

        updated.ShouldBeTrue();
        storage.DeletedKeys.ShouldBeEmpty();
        profile.PictureObjectKey.ShouldBe("new.jpg");
    }

    [Fact]
    public async Task Failed_Previous_File_Delete_Rolls_Back_Metadata()
    {
        var profile = UserProfileEntity.Create("identity-id", "jane@example.com", "Jane", DateTime.UtcNow);
        profile.PictureObjectKey = "new.jpg";
        var store = new RollbackStore(profile);
        var storage = new TrackingStorage("old.jpg");

        var exception = await Record.ExceptionAsync(
                            () => Endpoint.DeletePreviousOrRollbackAsync(
                                store,
                                storage,
                                profile.UserIdentityId,
                                "old.jpg",
                                "new.jpg",
                                Cancellation));

        exception.ShouldBeOfType<IOException>();
        profile.PictureObjectKey.ShouldBe("old.jpg");
        storage.DeletedKeys.ShouldContain("new.jpg");
    }

    static string AuthHeader(HttpResponseMessage rsp)
        => string.Join(" | ", rsp.Headers.WwwAuthenticate.Select(h => h.ToString()));

    static async Task<byte[]> CreateJpegBytesAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.CornflowerBlue);
        await using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);

        return ms.ToArray();
    }

    static async Task<byte[]> CreatePngBytesAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.LimeGreen);
        await using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms);

        return ms.ToArray();
    }

    static async Task<byte[]> CreateMultiFrameGifBytesAsync()
    {
        using var image = new Image<Rgba32>(100, 100, Color.Red);
        using var secondFrame = new Image<Rgba32>(100, 100, Color.Blue);
        image.Frames.AddFrame(secondFrame.Frames.RootFrame);
        await using var ms = new MemoryStream();
        await image.SaveAsGifAsync(ms);

        return ms.ToArray();
    }

    static async Task<byte[]> CreateGifBytesAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.MediumPurple);
        await using var ms = new MemoryStream();
        await image.SaveAsGifAsync(ms);

        return ms.ToArray();
    }

    async Task<(HttpResponseMessage rsp, Response? res)> UploadPictureAsync(string? accessToken,
                                                                            byte[] bytes,
                                                                            string fileName,
                                                                            string contentType,
                                                                            CancellationToken ct)
    {
        using var client = App.CreateClient(
            o =>
            {
                if (accessToken is not null)
                    o.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            });

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "File", fileName);

        var rsp = await client.PutAsync("/profiles/me/picture", content, ct);
        Response? res = null;

        if (rsp.IsSuccessStatusCode)
            res = await rsp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);

        return (rsp, res);
    }

    sealed class IndeterminateUpdateStore(UserProfileEntity profile) : IUserProfileStore
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
        public Task ActivateByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateDisplayNameAsync(string userIdentityId, string displayName, CancellationToken ct) => throw new NotSupportedException();
    }

    sealed class RollbackStore(UserProfileEntity profile) : IUserProfileStore
    {
        public Task<bool> TryUpdatePictureObjectKeyAsync(string userIdentityId,
                                                         string? expectedPictureObjectKey,
                                                         string? pictureObjectKey,
                                                         CancellationToken ct)
        {
            if (profile.PictureObjectKey != expectedPictureObjectKey)
                return Task.FromResult(false);

            profile.PictureObjectKey = pictureObjectKey;
            return Task.FromResult(true);
        }

        public Task<UserProfileEntity?> FindByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
            => Task.FromResult<UserProfileEntity?>(profile);

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct) => throw new NotSupportedException();
        public Task CreateAsync(UserProfileEntity value, CancellationToken ct) => throw new NotSupportedException();
        public Task ActivateByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateDisplayNameAsync(string userIdentityId, string displayName, CancellationToken ct) => throw new NotSupportedException();
    }

    sealed class TrackingStorage(string? failingKey = null) : IProfilePictureStorage
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> PutAsync(string userIdentityId, Stream image, string fileExtension, CancellationToken ct)
            => throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken ct)
        {
            if (objectKey == failingKey)
                throw new IOException("Simulated delete failure.");

            DeletedKeys.Add(objectKey);
            return Task.CompletedTask;
        }

        public string? BuildPublicUrl(string? objectKey, HttpRequest request) => throw new NotSupportedException();
    }
}
