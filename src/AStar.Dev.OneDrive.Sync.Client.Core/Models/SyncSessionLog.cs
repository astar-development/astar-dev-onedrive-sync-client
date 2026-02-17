using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

/// <summary>
///     Represents a summary of a sync session.
/// </summary>
/// <param name="Id">Unique identifier for the sync session.</param>
/// <param name="HashedAccountId">The hashed account identifier.</param>
/// <param name="StartedUtc">When the sync started.</param>
/// <param name="CompletedUtc">When the sync completed (null if still running).</param>
/// <param name="Status">Final status of the sync.</param>
/// <param name="FilesUploaded">Number of files uploaded.</param>
/// <param name="FilesDownloaded">Number of files downloaded.</param>
/// <param name="FilesDeleted">Number of files deleted.</param>
/// <param name="ConflictsDetected">Number of conflicts detected.</param>
/// <param name="TotalBytes">Total bytes transferred.</param>
public record SyncSessionLog(
    string Id,
    HashedAccountId HashedAccountId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    SyncStatus Status,
    int FilesUploaded,
    int FilesDownloaded,
    int FilesDeleted,
    int ConflictsDetected,
    long TotalBytes)
{
    public static SyncSessionLog CreateInitialRunning(string accountId, HashedAccountId hashedAccountId) => new(
        accountId,
        hashedAccountId,
        DateTime.UtcNow,
        null,
        SyncStatus.Running,
        0,
        0,
        0,
        0,
        0L);
}
