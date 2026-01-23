namespace AStar.Dev.OneDrive.Client.Core.Models.Enums;

/// <summary>
///     Represents the synchronization status of an individual file.
/// </summary>
public enum FileSyncStatus
{
    /// <summary>
    ///     Synchronization has occurred but no action has been performed.
    /// </summary>
    SyncOnly = 0,

    /// <summary>
    ///     File is in sync between local and OneDrive.
    /// </summary>
    Synced = 1,

    /// <summary>
    ///     File is pending upload to OneDrive.
    /// </summary>
    PendingUpload = 2,

    /// <summary>
    ///     File is pending download from OneDrive.
    /// </summary>
    PendingDownload = 3,

    /// <summary>
    ///     File is currently being uploaded.
    /// </summary>
    Uploading = 4,

    /// <summary>
    ///     File is currently being downloaded.
    /// </summary>
    Downloading = 5,

    /// <summary>
    ///     File has a conflict that needs resolution.
    /// </summary>
    Conflict = 6,

    /// <summary>
    ///     File synchronization failed.
    /// </summary>
    Failed = 7,

    /// <summary>
    ///     File has been deleted locally or remotely.
    /// </summary>
    Deleted = 8
}
