namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a file or folder in the synchronized file system.
/// </summary>
public class FileSystemItem
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account identifier this item belongs to.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OneDrive item ID from Graph API.
    /// </summary>
    public string DriveItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the item path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a folder.
    /// </summary>
    public bool? IsFolder { get; set; }

    /// <summary>
    /// Gets or sets the parent item identifier (NULL for root).
    /// </summary>
    public string? ParentItemId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is selected for sync.
    /// </summary>
    public bool IsSelected { get; set; } = false;

    /// <summary>
    /// Gets or sets the local file system path where this item is synced.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the remote (OneDrive) last modified timestamp.
    /// </summary>
    public DateTime? RemoteModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the local file system last modified timestamp.
    /// </summary>
    public DateTime? LocalModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the remote file hash for change detection.
    /// </summary>
    public string? RemoteHash { get; set; }

    /// <summary>
    /// Gets or sets the local file hash for change detection.
    /// </summary>
    public string? LocalHash { get; set; }

    /// <summary>
    /// Gets or sets the current sync status.
    /// Values: 'synced', 'pending_upload', 'pending_download', 'conflict', 'failed'.
    /// </summary>
    public SyncStatus SyncStatus { get; set; } = this.SyncStatus.None;

    /// <summary>
    /// Gets or sets the last sync direction.
    /// Values: 'upload', 'download', 'bidirectional'.
    /// </summary>
    public SyncDirection LastSyncDirection { get; set; } = SyncDirection.None;

    /// <summary>
    /// Gets or sets the navigation property to the associated account.
    /// </summary>
    public Account? Account { get; set; }
}
