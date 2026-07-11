using Microsoft.Extensions.Options;

namespace ProfilePictures;

sealed class LocalProfilePictureStorage(IOptions<UserProfileSettings> options, IHostEnvironment env) : IProfilePictureStorage
{
    public const string RequestPath = "/profile-pictures";

    readonly UserProfileSettings.ProfilePictureSettings _settings = options.Value.UserProfile.ProfilePictures;

    public async Task<string> PutAsync(string userIdentityId, Stream image, string fileExtension, CancellationToken ct)
    {
        var ext = NormalizeExtension(fileExtension);
        var version = Guid.NewGuid().ToString("N");
        var objectKey = $"profiles/{userIdentityId}/{version}.{ext}";
        var fullPath = ResolvePath(objectKey);
        var temporaryPath = $"{fullPath}.tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        try
        {
            await using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            {
                await image.CopyToAsync(file, ct);
                await file.FlushAsync(ct);
            }

            File.Move(temporaryPath, fullPath);

            return objectKey;
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return Task.CompletedTask;

        var fullPath = ResolvePath(objectKey);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public string? BuildPublicUrl(string? objectKey, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return null;

        var baseUrl = ResolvePublicBaseUrl(request);
        var key = objectKey.TrimStart('/');

        return $"{baseUrl}/{key}";
    }

    string ResolvePublicBaseUrl(HttpRequest request)
    {
        if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
            return _settings.PublicBaseUrl.TrimEnd('/');

        return $"{request.Scheme}://{request.Host.Value}{RequestPath}";
    }

    string ResolvePath(string objectKey)
    {
        var root = _settings.StorageRoot;
        var absoluteRoot = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(env.ContentRootPath, root));
        var fullPath = Path.GetFullPath(Path.Combine(absoluteRoot, objectKey.Replace('/', Path.DirectorySeparatorChar)));
        var relativePath = Path.GetRelativePath(absoluteRoot, fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
            relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid profile picture object key.");

        EnsureNoSymbolicLinks(absoluteRoot, relativePath);

        return fullPath;
    }

    static void EnsureNoSymbolicLinks(string absoluteRoot, string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var currentPath = absoluteRoot;

        for (var i = 0; i < segments.Length; i++)
        {
            currentPath = Path.Combine(currentPath, segments[i]);
            var linkTarget = i == segments.Length - 1
                                 ? new FileInfo(currentPath).LinkTarget
                                 : new DirectoryInfo(currentPath).LinkTarget;

            if (linkTarget is not null)
                throw new InvalidOperationException("Profile picture paths cannot contain symbolic links.");
        }
    }

    static string NormalizeExtension(string fileExtension)
    {
        var ext = fileExtension.Trim().TrimStart('.').ToLowerInvariant();

        return ext is "jpg" or "jpeg" or "png"
                   ? ext == "jpeg" ? "jpg" : ext
                   : throw new ArgumentOutOfRangeException(nameof(fileExtension), fileExtension, "Unsupported image extension.");
    }
}