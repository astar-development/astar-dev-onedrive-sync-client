namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Defines the resolution actions for sync conflicts.
/// </summary>
public enum ResolutionAction
{
    KeepLocal,
    KeepRemote,
    KeepBoth,
    Ignore
}
