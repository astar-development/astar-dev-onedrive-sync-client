using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncRepository
{
    // Jobs
    Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs);
    Task<List<SyncJobEntity>> GetPendingJobsAsync(string accountId);
    Task UpdateJobStateAsync(Guid jobId, SyncJobState state, string? error = null);
    Task ClearCompletedJobsAsync(string accountId);

    // Conflicts
    Task AddConflictAsync(SyncConflict conflict);
    Task<List<SyncConflictEntity>> GetPendingConflictsAsync(string accountId);
    Task ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution);
    Task<int> GetPendingConflictCountAsync(string accountId);
}
