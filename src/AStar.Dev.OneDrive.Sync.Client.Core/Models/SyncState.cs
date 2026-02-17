using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

/// <summary>
///     Represents the current state of synchronization for an account.
/// </summary>
/// <param name="AccountId">The account identifier.</param>
/// <param name="HashedAccountId">The hashed account identifier.</param>
/// <param name="Status">Current synchronization status.</param>
/// <param name="TotalFiles">Total number of files to synchronize.</param>
/// <param name="CompletedFiles">Number of files already synchronized.</param>
/// <param name="TotalBytes">Total bytes to synchronize.</param>
/// <param name="CompletedBytes">Number of bytes already synchronized.</param>
/// <param name="FilesDownloading">Number of files currently downloading.</param>
/// <param name="FilesUploading">Number of files currently uploading.</param>
/// <param name="FilesDeleted">Number of files deleted during sync.</param>
/// <param name="ConflictsDetected">Number of conflicts detected.</param>
/// <param name="MegabytesPerSecond">Current transfer speed in MB/s.</param>
/// <param name="EstimatedSecondsRemaining">Estimated seconds until completion.</param>
/// <param name="CurrentStatusMessage">The current Status Message (e.g. current folder - null when not scanning).</param>
/// <param name="LastUpdateUtc">Timestamp of the last state update.</param>
public sealed record SyncState(
    string AccountId,
    HashedAccountId HashedAccountId,
    SyncStatus Status,
    int TotalFiles,
    int CompletedFiles,
    long TotalBytes,
    long CompletedBytes,
    int FilesDownloading,
    int FilesUploading,
    int FilesDeleted,
    int ConflictsDetected,
    double MegabytesPerSecond,
    int? EstimatedSecondsRemaining = 0,
    string? CurrentStatusMessage = "",
    DateTimeOffset? LastUpdateUtc = null
)
{
    public static SyncState CreateInitial(string accountId, HashedAccountId hashedAccountId) => new(accountId, hashedAccountId, SyncStatus.Idle, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "", DateTimeOffset.UtcNow);

    public static SyncState Create(string accountId, HashedAccountId hashedAccountId, SyncStatus syncStatus, string message) => new(accountId, hashedAccountId, syncStatus, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, message, DateTimeOffset.UtcNow);

    public static SyncState CreateFailed(string accountId, HashedAccountId hashedAccountId, int totalItemsProcessed, string message) => new(accountId, hashedAccountId, SyncStatus.Failed, totalItemsProcessed, 0, 0, 0, 0, 0, 0, 0, 0, 0, message, DateTimeOffset.UtcNow);
}
