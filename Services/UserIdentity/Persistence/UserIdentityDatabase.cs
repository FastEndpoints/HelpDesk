using Common.StorageProvider;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Persistence;

static class UserIdentityDatabase
{
    static UserIdentityDatabase()
    {
        BsonSerializer.TryRegisterSerializer(new ObjectSerializer(_ => true));
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public static async Task InitializeAsync(DB db)
    {
        await db.Index<UserIdentityEntity>()
                .Key(i => i.NormalizedEmail, KeyType.Ascending)
                .Option(o => o.Unique = true)
                .CreateAsync();

        await db.Index<UserIdentityEntity>()
                .Key(i => i.VerificationCode, KeyType.Ascending)
                .Option(o =>
                {
                    o.Unique = true;
                    o.Sparse = true;
                })
                .CreateAsync();

        await db.Index<EventRecord>()
                .Key(r => r.EventType, KeyType.Ascending)
                .Key(r => r.SubscriberID, KeyType.Ascending)
                .Key(r => r.IsComplete, KeyType.Ascending)
                .Key(r => r.ExpireOn, KeyType.Ascending)
                .CreateAsync();
    }
}