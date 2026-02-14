using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Coordinates sync state management and progress reporting.
/// </summary>
public interface ISyncStateCoordinator
{
    /// <summary>
    ///     Gets an observable stream of sync state updates.
    /// </summary>
    IObservable<SyncState> Progress { get; }

    /// <summary>
    ///     Initializes a new sync session.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="enableDetailedLogging">Whether to enable detailed sync logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session ID if logging is enabled, otherwise null.</returns>
    Task<string?> InitializeSessionAsync(string accountId, bool enableDetailedLogging, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates and reports sync progress.
    /// </summary>
    /// <param name="accountId">Account identifier.</param>
    /// <param name="status">Current sync status.</param>
    /// <param name="totalFiles">Total number of files to synchronize.</param>
    /// <param name="completedFiles">Number of files already synchronized.</param>
    /// <param name="totalBytes">Total bytes to synchronize.</param>
    /// <param name="completedBytes">Number of bytes already synchronized.</param>
    /// <param name="filesDownloading">Number of files currently downloading.</param>
    /// <param name="filesUploading">Number of files currently uploading.</param>
    /// <param name="filesDeleted">Number of files deleted during sync.</param>
    /// <param name="conflictsDetected">Number of conflicts detected.</param>
    /// <param name="currentScanningFolder">The current folder being scanned (null when not scanning).</param>
    /// <param name="phaseTotalBytes">Total bytes for the current phase (upload/download).</param>
    void UpdateProgress(
        string accountId,
        SyncStatus status,
        int totalFiles = 0,
        int completedFiles = 0,
        long totalBytes = 0,
        long completedBytes = 0,
        int filesDownloading = 0,
        int filesUploading = 0,
        int filesDeleted = 0,
        int conflictsDetected = 0,
        string? currentScanningFolder = null,
        long? phaseTotalBytes = null);

    /// <summary>
    ///     Records successful completion of a sync session.
    /// </summary>
    /// <param name="uploadCount">Number of files uploaded.</param>
    /// <param name="downloadCount">Number of files downloaded.</param>
    /// <param name="deleteCount">Number of files deleted.</param>
    /// <param name="conflictCount">Number of conflicts detected.</param>
    /// <param name="completedBytes">Total bytes transferred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordCompletionAsync(int uploadCount, int downloadCount, int deleteCount, int conflictCount, long completedBytes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records sync failure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordFailureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records sync cancellation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordCancellationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current sync state.
    /// </summary>
    /// <returns>The current sync state.</returns>
    SyncState GetCurrentState();

    /// <summary>
    ///     Gets the current session ID.
    /// </summary>
    /// <returns>The current session ID, or null if no session is active.</returns>
    string? GetCurrentSessionId();

    /// <summary>
    ///     Resets progress tracking details.
    /// </summary>
    /// <param name="completedBytes">The number of bytes completed so far.</param>
    void ResetTrackingDetails(long completedBytes = 0);
}
