namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models;

/// <summary>
/// Represents a change detected from the Microsoft Graph API delta query.
/// </summary>
public class DeltaChange
{
    /// <summary>
    /// Gets or sets the unique identifier of the changed item.
    /// </summary>
    public required string DriveItemId { get; init; }

    /// <summary>
    /// Gets or sets the name of the changed item.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the path of the changed item.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or sets whether the item is a folder.
    /// </summary>
    public required bool IsFolder { get; init; }

    /// <summary>
    /// Gets or sets the type of change.
    /// </summary>
    public required ChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets or sets the remote modification timestamp.
    /// </summary>
    public DateTime? RemoteModifiedAt { get; init; }

    /// <summary>
    /// Gets or sets the remote hash for change detection.
    /// </summary>
    public string? RemoteHash { get; init; }
}
