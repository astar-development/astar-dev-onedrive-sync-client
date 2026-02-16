using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for detecting file synchronization conflicts between local and remote files.
/// </summary>
public interface IConflictDetectionService
{
    /// <summary>
    ///     Checks for conflicts when processing a known remote file (file already in database).
    /// </summary>
    /// <param name="hashedAccountId">Hashed account identifier.</param>
    /// <param name="remoteFile">Remote file metadata from OneDrive.</param>
    /// <param name="existingFile">Existing file metadata from database.</param>
    /// <param name="localFilesDict">Dictionary of local files keyed by relative path.</param>
    /// <param name="localSyncPath">Local sync folder path.</param>
    /// <param name="sessionId">Current sync session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (HasConflict, FileToDownload). FileToDownload is null if conflict or no change detected.</returns>
    Task<(bool HasConflict, FileMetadata? FileToDownload)> CheckKnownFileConflictAsync(
        HashedAccountId hashedAccountId,
        DriveItemEntity remoteFile,
        DriveItemEntity existingFile,
        Dictionary<string, FileMetadata> localFilesDict,
        string? localSyncPath,
        string? sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Checks for conflicts when processing a file during first sync or new file scenario.
    /// </summary>
    /// <param name="hashedAccountId">Hashed account identifier.</param>
    /// <param name="remoteFile">Remote file metadata from OneDrive.</param>
    /// <param name="localFilesDict">Dictionary of local files keyed by relative path.</param>
    /// <param name="localSyncPath">Local sync folder path.</param>
    /// <param name="sessionId">Current sync session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (HasConflict, FileToDownload, MatchedFile). Returns matched file if files are identical.</returns>
    Task<(bool HasConflict, FileMetadata? FileToDownload, FileMetadata? MatchedFile)> CheckFirstSyncFileConflictAsync(
        HashedAccountId hashedAccountId,
        DriveItemEntity remoteFile,
        Dictionary<string, FileMetadata> localFilesDict,
        string? localSyncPath,
        string? sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if a local file has changed since last sync.
    /// </summary>
    /// <param name="relativePath">Relative path of the file.</param>
    /// <param name="existingFile">Existing file metadata from database.</param>
    /// <param name="localFilesDict">Dictionary of local files keyed by relative path.</param>
    /// <returns>True if local file has changed (timestamp or size difference), false otherwise.</returns>
    bool CheckIfLocalFileHasChanged(
        string relativePath,
        DriveItemEntity existingFile,
        Dictionary<string, FileMetadata> localFilesDict);

    /// <summary>
    ///     Records a synchronization conflict to the database.
    /// </summary>
    /// <param name="hashedAccountId">Hashed account identifier.</param>
    /// <param name="remoteFile">Remote file metadata.</param>
    /// <param name="localFile">Local file metadata.</param>
    /// <param name="sessionId">Current sync session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordSyncConflictAsync(
        HashedAccountId hashedAccountId,
        DriveItemEntity remoteFile,
        FileMetadata localFile,
        string? sessionId,
        CancellationToken cancellationToken);
}
