using FastEndpoints;
using MongoDB.Entities;

namespace Common.StorageProvider;

public sealed class EventRecord : Entity, IEventStorageRecord
{
    public string SubscriberID { get; set; } = null!;
    public Guid TrackingID { get; set; }
    public object Event { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public DateTime ExpireOn { get; set; }
    public bool IsComplete { get; set; }
}