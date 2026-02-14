using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for handling file and folder deletions during synchronization.
/// </summary>
public interface IDeletionSyncService
{
    /// <summary>
    ///     Processes files that were deleted from OneDrive but still exist locally.
    ///     Deletes local copies and removes records from the database.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="existingFiles">List of files currently tracked in the database.</param>
    /// <param name="remotePathsSet">Set of paths that exist on OneDrive.</param>
    /// <param name="localPathsSet">Set of paths that exist locally.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessRemoteToLocalDeletionsAsync(
        string accountId,
        IReadOnlyList<DriveItemEntity> existingFiles,
        HashSet<string> remotePathsSet,
        HashSet<string> localPathsSet,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Processes files that were deleted locally but still exist on OneDrive.
    ///     Deletes remote copies and removes records from the database.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="allLocalFiles">List of all local files tracked in the database.</param>
    /// <param name="remotePathsSet">Set of paths that exist on OneDrive.</param>
    /// <param name="localPathsSet">Set of paths that exist locally.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessLocalToRemoteDeletionsAsync(
        string accountId,
        List<FileMetadata> allLocalFiles,
        HashSet<string> remotePathsSet,
        HashSet<string> localPathsSet,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Removes deleted file records from the database.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="itemsToDelete">List of items to delete from the database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupDatabaseRecordsAsync(
        string accountId,
        IEnumerable<DriveItemEntity> itemsToDelete,
        CancellationToken cancellationToken);
}
