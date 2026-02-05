namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Models;

/// <summary>
/// Represents a detected change in the local file system.
/// </summary>
public class LocalChange
{
    /// <summary>
    /// Gets the full path of the file or directory that changed.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public required LocalChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the change was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the original file path for rename operations.
    /// </summary>
    public string? OldFilePath { get; init; }
}
