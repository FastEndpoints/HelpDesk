using MongoDB.Driver;

namespace Persistence;

sealed class MongoDisplayNameStore : IDisplayNameStore
{
    public async Task UpsertAsync(string userIdentityId, string displayName, DateTime updatedAt, CancellationToken ct)
    {
        var existing = await FindEntityAsync(userIdentityId, ct);

        if (existing is null)
        {
            try
            {
                await DB.Default.InsertAsync(
                    new DisplayNameEntity
                    {
                        UserIdentityId = userIdentityId,
                        DisplayName = displayName,
                        UpdatedAt = updatedAt
                    },
                    ct);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                existing = await FindEntityAsync(userIdentityId, ct);

                if (existing is null)
                    throw;

                await ApplyUpdateAsync(existing, displayName, updatedAt, ct);
            }

            return;
        }

        await ApplyUpdateAsync(existing, displayName, updatedAt, ct);
    }

    public async Task<string?> FindAsync(string userIdentityId, CancellationToken ct)
    {
        var entity = await FindEntityAsync(userIdentityId, ct);

        return entity?.DisplayName;
    }

    static async Task<DisplayNameEntity?> FindEntityAsync(string userIdentityId, CancellationToken ct)
    {
        var matches = await DB.Default
                              .Find<DisplayNameEntity>()
                              .Match(e => e.UserIdentityId == userIdentityId)
                              .Limit(1)
                              .ExecuteAsync(ct);

        return matches.SingleOrDefault();
    }

    static async Task ApplyUpdateAsync(DisplayNameEntity existing,
                                       string displayName,
                                       DateTime updatedAt,
                                       CancellationToken ct)
    {
        // Ignore stale/out-of-order projections.
        if (existing.UpdatedAt > updatedAt)
            return;

        existing.DisplayName = displayName;
        existing.UpdatedAt = updatedAt;
        await DB.Default.SaveAsync(existing, ct);
    }
}
