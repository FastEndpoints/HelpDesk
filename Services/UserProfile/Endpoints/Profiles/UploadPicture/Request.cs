using FluentValidation;
using Microsoft.Extensions.Options;

namespace Endpoints.Profiles.UploadPicture;

sealed class Request
{
    public IFormFile File { get; set; } = null!;

    internal sealed class Validator : Validator<Request>
    {
        static readonly HashSet<string> _allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

        static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public Validator(IOptions<UserProfileSettings> options)
        {
            var maxUploadBytes = options.Value.UserProfile.ProfilePictures.MaxUploadBytes;

            RuleFor(x => x.File)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("A picture file is required.")
                .Must(file => file.Length > 0)
                .WithMessage("Picture file is empty.")
                .Must(file => file.Length <= maxUploadBytes)
                .WithMessage("Picture exceeds the configured upload limit.")
                .Must(BeAllowedType)
                .WithMessage("Only PNG and JPG images are allowed.");
        }

        static bool BeAllowedType(IFormFile file)
        {
            var contentTypeOk = !string.IsNullOrWhiteSpace(file.ContentType) && _allowedContentTypes.Contains(file.ContentType.Split(';', 2)[0].Trim());
            var extension = Path.GetExtension(file.FileName);
            var extensionOk = !string.IsNullOrWhiteSpace(extension) && _allowedExtensions.Contains(extension);

            return contentTypeOk || extensionOk;
        }
    }
}