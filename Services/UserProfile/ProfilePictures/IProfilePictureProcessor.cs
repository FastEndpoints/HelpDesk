namespace ProfilePictures;

interface IProfilePictureProcessor
{
    /// <summary>
    /// Decodes, center-crops, and resizes to 300×300. Returns encoded bytes and extension (png/jpg).
    /// </summary>
    Task<ProcessedProfilePicture> ProcessAsync(Stream source, CancellationToken ct);
}

readonly record struct ProcessedProfilePicture(byte[] Bytes, string Extension, string ContentType);
