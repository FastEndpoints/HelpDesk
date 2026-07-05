using Common.StorageProvider;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Persistence;

static class UserProfileDatabase
{
    static UserProfileDatabase()
    {
        BsonSerializer.TryRegisterSerializer(new ObjectSerializer(_ => true));
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public static async Task InitializeAsync(DB db)
    {
        await db.Index<UserProfileEntity>()
                .Key(p => p.NormalizedEmail, KeyType.Ascending)
                .Option(o => o.Unique = true)
                .CreateAsync();

        await db.Index<EventRecord>()
                .Key(r => r.EventType, KeyType.Ascending)
                .Key(r => r.SubscriberID, KeyType.Ascending)
                .Key(r => r.IsComplete, KeyType.Ascending)
                .Key(r => r.ExpireOn, KeyType.Ascending)
                .CreateAsync();
    }
}