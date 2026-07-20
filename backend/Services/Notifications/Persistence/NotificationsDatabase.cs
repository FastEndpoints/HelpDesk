using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Persistence;

static class NotificationsDatabase
{
    static NotificationsDatabase()
    {
        BsonSerializer.TryRegisterSerializer(new ObjectSerializer(_ => true));
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public static async Task InitializeAsync(DB db)
    {
        await db.Index<EventRecord>()
                .Key(r => r.EventType, KeyType.Ascending)
                .Key(r => r.SubscriberID, KeyType.Ascending)
                .Key(r => r.IsComplete, KeyType.Ascending)
                .Key(r => r.ExpireOn, KeyType.Ascending)
                .CreateAsync();

        await db.Index<JobRecord>()
                .Key(r => r.QueueID, KeyType.Ascending)
                .Key(r => r.IsComplete, KeyType.Ascending)
                .Key(r => r.ExecuteAfter, KeyType.Ascending)
                .Key(r => r.ExpireOn, KeyType.Ascending)
                .CreateAsync();

        await db.Index<JobRecord>()
                .Key(r => r.TrackingID, KeyType.Ascending)
                .CreateAsync();

        // Unique while row exists (incl. completed); null/empty keys excluded so non-idempotent jobs do not collide.
        await db.Index<JobRecord>()
                .Key(r => r.QueueID, KeyType.Ascending)
                .Key(r => r.IdempotencyKey, KeyType.Ascending)
                .Option(o =>
                {
                    o.Unique = true;
                    o.PartialFilterExpression = new BsonDocument(
                        "IdempotencyKey",
                        new BsonDocument
                        {
                            { "$type", "string" },
                            { "$gt", "" }
                        });
                })
                .CreateAsync();

        await db.Index<DisplayNameEntity>()
                .Key(e => e.UserIdentityId, KeyType.Ascending)
                .Option(o => o.Unique = true)
                .CreateAsync();
    }
}