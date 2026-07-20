namespace Persistence;

[MongoDB.Entities.Collection("DisplayNames")]
sealed class DisplayNameEntity : Entity
{
    public string UserIdentityId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
}
