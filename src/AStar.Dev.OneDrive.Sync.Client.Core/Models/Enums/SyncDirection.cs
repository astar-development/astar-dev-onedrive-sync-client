namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

/// <summary>
///     Represents the direction of file synchronization.
/// </summary>
public enum SyncDirection
{
    /// <summary>
    ///     File has been added to the database but not synced to/from OneDrive.
    /// </summary>
    None = 0,

    /// <summary>
    ///     File was uploaded to OneDrive.
    /// </summary>
    Upload = 1,

    /// <summary>
    ///     File was downloaded from OneDrive.
    /// </summary>
    Download = 2
}
