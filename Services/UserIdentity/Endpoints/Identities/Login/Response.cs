namespace Endpoints.Identities.Login;

sealed class Response
{
    public string Id { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string AccessToken { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}