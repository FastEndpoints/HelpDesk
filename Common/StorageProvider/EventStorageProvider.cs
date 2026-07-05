using FastEndpoints;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

namespace Common.StorageProvider;

public sealed class EventStorageProvider : IEventHubStorageProvider<EventRecord>, IEventSubscriberStorageProvider<EventRecord>
{
    public async ValueTask<IEnumerable<string>> RestoreSubscriberIDsForEventTypeAsync(SubscriberIDRestorationParams<EventRecord> p)
    {
        var subscriberIds = await DB.Default
            .Queryable<EventRecord>()
            .Where(p.Match)
            .Select(p.Projection)
            .Distinct()
            .ToListAsync(p.CancellationToken);

        return subscriberIds;
    }

    public async ValueTask StoreEventsAsync(IEnumerable<EventRecord> records, CancellationToken ct)
        => await DB.Default.SaveAsync(records, ct);

    public async ValueTask StoreEventAsync(EventRecord record, CancellationToken ct)
        => await DB.Default.SaveAsync(record, ct);

    public async ValueTask<IEnumerable<EventRecord>> GetNextBatchAsync(PendingRecordSearchParams<EventRecord> parameters)
    {
        var records = await DB.Default
            .Find<EventRecord>()
            .Match(parameters.Match)
            .Sort(r => r.ExpireOn, Order.Ascending)
            .Limit(parameters.Limit)
            .ExecuteAsync(parameters.CancellationToken);

        return records;
    }

    public async ValueTask MarkEventAsCompleteAsync(EventRecord record, CancellationToken ct)
        => await DB.Default
            .Update<EventRecord>()
            .MatchID(record.ID)
            .Modify(r => r.IsComplete, true)
            .ExecuteAsync(ct);

    public async ValueTask PurgeStaleRecordsAsync(StaleRecordSearchParams<EventRecord> parameters)
        => await DB.Default.DeleteAsync(parameters.Match, parameters.CancellationToken);
}