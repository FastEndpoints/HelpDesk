namespace Endpoints.Profiles.DeletePicture;

sealed class Response
{
    public string Id { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? PictureUrl { get; init; }
}
