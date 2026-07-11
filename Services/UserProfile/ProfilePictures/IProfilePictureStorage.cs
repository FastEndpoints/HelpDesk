using Microsoft.AspNetCore.Http;

namespace ProfilePictures;

interface IProfilePictureStorage
{
    /// <summary>
    /// Writes processed image bytes under a versioned key and returns the relative object key.
    /// </summary>
    Task<string> PutAsync(string userIdentityId, Stream image, string fileExtension, CancellationToken ct);

    /// <summary>
    /// Deletes the file for the given object key if present.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken ct);

    /// <summary>
    /// Builds a public picture URL for the object key, or null when key is missing.
    /// Uses optional configured public base when set; otherwise current request scheme/host + /profile-pictures.
    /// </summary>
    string? BuildPublicUrl(string? objectKey, HttpRequest request);
}
