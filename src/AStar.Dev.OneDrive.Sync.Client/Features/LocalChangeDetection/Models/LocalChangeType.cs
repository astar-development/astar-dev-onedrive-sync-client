namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Models;

/// <summary>
/// Defines the types of local file system changes that can be detected.
/// </summary>
public enum LocalChangeType
{
    /// <summary>
    /// A new file or directory was created.
    /// </summary>
    Added,

    /// <summary>
    /// An existing file or directory was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A file or directory was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A file or directory was renamed.
    /// </summary>
    Renamed
}
