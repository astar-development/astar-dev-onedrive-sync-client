namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Defines the synchronization status of a file system item.
/// </summary>
public enum SyncStatus
{
    None,
    Synced,
    PendingUpload,
    PendingDownload,
    Conflict,
    Failed
}
