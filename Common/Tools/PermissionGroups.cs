namespace Common.Tools;

/// <summary>
/// Shared permission group names for the mesh.
/// Group name ≡ JWT role claim value ≡ FastEndpoints AccessControl groupName ≡ Allow.{Name}.
/// Registry holds names only; resource services expand groups to local permission codes.
/// </summary>
public static class PermissionGroups
{
    public const string User = nameof(User);
    public const string Admin = nameof(Admin);

    public static IReadOnlyList<string> All { get; } = [User, Admin];
    public static IReadOnlyList<string> Defaults { get; } = [User];
}
