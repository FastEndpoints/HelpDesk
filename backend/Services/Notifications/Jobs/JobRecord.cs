namespace Jobs;

public sealed class JobRecord : Entity, IJobStorageRecord, IHasIdempotencyKey
{
    public Guid TrackingID { get; set; }
    public string QueueID { get; set; } = null!;
    public object Command { get; set; } = null!;
    public DateTime ExecuteAfter { get; set; }
    public DateTime ExpireOn { get; set; }
    public bool IsComplete { get; set; }
    public string? IdempotencyKey { get; set; }
}