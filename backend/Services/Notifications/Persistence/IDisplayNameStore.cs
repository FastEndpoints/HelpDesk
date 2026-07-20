namespace Persistence;

interface IDisplayNameStore
{
    Task UpsertAsync(string userIdentityId, string displayName, DateTime updatedAt, CancellationToken ct);
    Task<string?> FindAsync(string userIdentityId, CancellationToken ct);
}
