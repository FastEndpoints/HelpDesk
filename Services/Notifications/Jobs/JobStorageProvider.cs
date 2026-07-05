namespace Jobs;

public sealed class JobStorageProvider : IJobStorageProvider<JobRecord>
{
    public bool DistributedJobProcessingEnabled => false;

    public async Task StoreJobAsync(JobRecord job, CancellationToken ct)
        => await DB.Default.SaveAsync(job, ct);

    public async Task<ICollection<JobRecord>> GetNextBatchAsync(PendingJobSearchParams<JobRecord> p)
    {
        var jobs = await DB.Default
                           .Find<JobRecord>()
                           .Match(p.Match)
                           .Sort(r => r.ExecuteAfter, MongoDB.Entities.Order.Ascending)
                           .Limit(p.Limit)
                           .ExecuteAsync(p.CancellationToken);

        return jobs;
    }

    public async Task MarkJobAsCompleteAsync(JobRecord job, CancellationToken ct)
        => await DB.Default
                   .Update<JobRecord>()
                   .MatchID(job.ID)
                   .Modify(r => r.IsComplete, true)
                   .ExecuteAsync(ct);

    public async Task CancelJobAsync(Guid trackingId, CancellationToken ct)
        => await DB.Default
                   .Update<JobRecord>()
                   .Match(r => r.TrackingID == trackingId)
                   .Modify(r => r.IsComplete, true)
                   .ExecuteAsync(ct);

    public async Task OnHandlerExecutionFailureAsync(JobRecord job, Exception _, CancellationToken ct)
        => await DB.Default
                   .Update<JobRecord>()
                   .MatchID(job.ID)
                   .Modify(r => r.ExecuteAfter, DateTime.UtcNow.AddMinutes(1))
                   .ExecuteAsync(ct);

    public async Task PurgeStaleJobsAsync(StaleJobSearchParams<JobRecord> p)
        => await DB.Default.DeleteAsync(p.Match, p.CancellationToken);
}