namespace Persistence;

sealed class MongoPasswordResetTokenStore : IPasswordResetTokenStore
{
    public async Task<PasswordResetTokenEntity?> FindByTokenHashAsync(string tokenHash, CancellationToken ct)
        => await DB.Default
                   .Find<PasswordResetTokenEntity>()
                   .Match(t => t.TokenHash == tokenHash)
                   .ExecuteSingleAsync(ct);

    public async Task<PasswordResetTokenEntity?> FindLatestByUserIdentityIdAsync(string userIdentityId, CancellationToken ct)
    {
        var tokens = await DB.Default
                             .Find<PasswordResetTokenEntity>()
                             .Match(t => t.UserIdentityId == userIdentityId)
                             .Sort(t => t.CreatedAt, MongoDB.Entities.Order.Descending)
                             .Limit(1)
                             .ExecuteAsync(ct);

        return tokens.SingleOrDefault();
    }

    public Task CreateAsync(PasswordResetTokenEntity token, CancellationToken ct)
        => DB.Default.InsertAsync(token, ct);

    public Task DeleteByUserIdentityIdAsync(string userIdentityId, CancellationToken _)
    {
        return DB.Default.DeleteAsync<PasswordResetTokenEntity>(t => t.UserIdentityId == userIdentityId);
    }
}