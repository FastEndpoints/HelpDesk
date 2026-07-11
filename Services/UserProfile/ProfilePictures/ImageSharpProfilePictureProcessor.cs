using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace ProfilePictures;

sealed class ImageSharpProfilePictureProcessor : IProfilePictureProcessor
{
    public const int SizePx = 300;
    public const int MaxInputDimensionPx = 4096;
    public const long MaxInputPixels = 16_000_000;

    public async Task<ProcessedProfilePicture> ProcessAsync(Stream source, CancellationToken ct)
    {
        var startPosition = source.CanSeek ? source.Position : 0;
        Stream input = source;

        if (!source.CanSeek)
        {
            var buffered = new MemoryStream();
            await source.CopyToAsync(buffered, ct);
            buffered.Position = 0;
            input = buffered;
        }

        await using var ownedInput = input == source ? null : input;
        var identifyOptions = new DecoderOptions { MaxFrames = 2, SkipMetadata = true };
        var info = await Image.IdentifyAsync(identifyOptions, input, ct);

        if (info.Width > MaxInputDimensionPx ||
            info.Height > MaxInputDimensionPx ||
            (long)info.Width * info.Height > MaxInputPixels ||
            info.FrameMetadataCollection.Count > 1)
            throw new InvalidImageFormatException("Picture dimensions or frame count exceed the allowed limit.");

        var format = info.Metadata.DecodedImageFormat switch
        {
            PngFormat => ImageFormat.Png,
            JpegFormat => ImageFormat.Jpeg,
            _ => throw new InvalidImageFormatException("Only PNG and JPG images are allowed.")
        };

        input.Position = startPosition;

        var loadOptions = new DecoderOptions { MaxFrames = 1, SkipMetadata = true };
        using var image = await Image.LoadAsync(loadOptions, input, ct);

        image.Mutate(
            ctx =>
            {
                ctx.Resize(
                    new ResizeOptions
                    {
                        Size = new(SizePx, SizePx),
                        Mode = ResizeMode.Crop,
                        Position = AnchorPositionMode.Center
                    });
            });

        await using var output = new MemoryStream();

        if (format == ImageFormat.Png)
        {
            await image.SaveAsPngAsync(output, new(), ct);

            return new(output.ToArray(), "png", "image/png");
        }

        await image.SaveAsJpegAsync(output, new() { Quality = 85 }, ct);

        return new(output.ToArray(), "jpg", "image/jpeg");
    }

    enum ImageFormat
    {
        Jpeg,
        Png
    }
}

sealed class InvalidImageFormatException(string message) : Exception(message);