namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Defines the types of sync conflicts.
/// </summary>
public enum ConflictType
{
    None,
    LocalNewer,
    RemoteNewer,
    BothModified
}
