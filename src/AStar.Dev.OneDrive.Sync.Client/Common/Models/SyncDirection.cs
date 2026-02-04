namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Defines the synchronization direction of a file system item.
/// </summary>
public enum SyncDirection
{
    None,
    Upload,
    Download,
    Bidirectional
}
