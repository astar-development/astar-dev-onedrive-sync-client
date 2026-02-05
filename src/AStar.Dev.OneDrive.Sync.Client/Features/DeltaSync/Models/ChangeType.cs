namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models;

/// <summary>
/// Represents the type of change detected in a delta sync operation.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// A new item was added.
    /// </summary>
    Added,

    /// <summary>
    /// An existing item was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// An item was deleted.
    /// </summary>
    Deleted
}
