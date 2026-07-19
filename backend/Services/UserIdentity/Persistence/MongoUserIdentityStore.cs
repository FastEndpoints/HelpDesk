using MongoDB.Driver;

namespace Persistence;

sealed class MongoUserIdentityStore : IUserIdentityStore
{
    public async Task<UserIdentityEntity?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        => await DB.Default
                   .Find<UserIdentityEntity>()
                   .Match(i => i.NormalizedEmail == normalizedEmail)
                   .ExecuteSingleAsync(ct);

    public async Task<UserIdentityEntity?> FindByVerificationCodeAsync(string verificationCode, CancellationToken ct)
        => await DB.Default
                   .Find<UserIdentityEntity>()
                   .Match(i => i.VerificationCode == verificationCode)
                   .ExecuteSingleAsync(ct);

    public async Task CreateAsync(UserIdentityEntity identity, CancellationToken ct)
    {
        try
        {
            await DB.Default.InsertAsync(identity, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateIdentityEmailException(identity.NormalizedEmail);
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            throw new DuplicateIdentityEmailException(identity.NormalizedEmail);
        }
    }

    public async Task ActivateAsync(string id, CancellationToken ct)
        => await DB.Default
                   .Update<UserIdentityEntity>()
                   .MatchID(id)
                   .Modify(i => i.Status, UserIdentityStatus.Active)
                   .ExecuteAsync(ct);

    public async Task ReplaceVerificationCodeAsync(string id, string verificationCode, DateTime issuedAt, CancellationToken ct)
        => await DB.Default
                   .Update<UserIdentityEntity>()
                   .MatchID(id)
                   .Modify(i => i.VerificationCode, verificationCode)
                   .Modify(i => i.VerificationIssuedAt, issuedAt)
                   .ExecuteAsync(ct);
}