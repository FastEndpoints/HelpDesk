using Common.Tools;
using MongoDB.Driver;

namespace Persistence;

sealed class MongoUserProfileStore : IUserProfileStore
{
    public async Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct)
        => await DB.Default
                   .Find<UserProfileEntity>()
                   .Match(p => p.NormalizedEmail == normalizedEmail)
                   .ExecuteAnyAsync(ct);

    public async Task CreateAsync(UserProfileEntity profile, CancellationToken ct)
    {
        try
        {
            await DB.Default.InsertAsync(profile, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateEmailException(profile.NormalizedEmail);
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            throw new DuplicateEmailException(profile.NormalizedEmail);
        }
    }

    public async Task ActivateByEmailAsync(string email, CancellationToken ct)
        => await DB.Default
                   .Update<UserProfileEntity>()
                   .Match(p => p.NormalizedEmail == email.NormalizeForLookup())
                   .Modify(p => p.Status, UserProfileStatus.Active)
                   .Modify(p => p.EmailVerified, true)
                   .ExecuteAsync(ct);
}