using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Mappers;

/// <summary>
/// Maps DeltaChange objects to FileSystemItem entities.
/// </summary>
public static class DeltaChangeMapper
{
    /// <summary>
    /// Maps a DeltaChange to a FileSystemItem entity.
    /// </summary>
    /// <param name="change">The delta change from Graph API.</param>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <param name="existingItem">Optional existing FileSystemItem for hash comparison.</param>
    /// <returns>A FileSystemItem entity mapped from the delta change.</returns>
    public static FileSystemItem ToFileSystemItem(this DeltaChange change, string hashedAccountId, FileSystemItem? existingItem = null)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedAccountId);

        SyncStatus syncStatus = DetermineSyncStatus(change, existingItem);

        return new FileSystemItem
        {
            Id = existingItem?.Id ?? Guid.NewGuid().ToString(),
            HashedAccountId = hashedAccountId,
            DriveItemId = change.DriveItemId,
            Name = change.Name,
            Path = change.Path,
            IsFolder = change.IsFolder,
            RemoteModifiedAt = change.RemoteModifiedAt,
            RemoteHash = change.RemoteHash,
            SyncStatus = syncStatus,
            LastSyncDirection = existingItem?.LastSyncDirection ?? SyncDirection.None,
            LocalPath = existingItem?.LocalPath,
            LocalModifiedAt = existingItem?.LocalModifiedAt,
            LocalHash = existingItem?.LocalHash
        };
    }

    private static SyncStatus DetermineSyncStatus(DeltaChange change, FileSystemItem? existingItem) => change.ChangeType switch
    {
        ChangeType.Deleted => SyncStatus.PendingDownload,
        ChangeType.Added => SyncStatus.PendingDownload,
        ChangeType.Modified when HasRemoteChanges(change, existingItem) => SyncStatus.PendingDownload,
        ChangeType.Modified => SyncStatus.Synced,
        _ => SyncStatus.None
    };

    private static bool HasRemoteChanges(DeltaChange change, FileSystemItem? existingItem)
        => existingItem switch
        {
            null => true,
            _ => !change.IsFolder && (HasEmptyRemoteHash(change, existingItem) || RemoteHashHasChanged(change, existingItem)),
        };

    private static bool RemoteHashHasChanged(DeltaChange change, FileSystemItem existingItem) => !string.Equals(change.RemoteHash, existingItem.RemoteHash, StringComparison.Ordinal);
    
    private static bool HasEmptyRemoteHash(DeltaChange change, FileSystemItem existingItem) => string.IsNullOrEmpty(change.RemoteHash) || string.IsNullOrEmpty(existingItem.RemoteHash);
}
