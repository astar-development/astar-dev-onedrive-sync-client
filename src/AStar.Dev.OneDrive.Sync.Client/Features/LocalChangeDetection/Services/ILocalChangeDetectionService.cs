using AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

/// <summary>
/// Defines operations for detecting and tracking local file system changes.
/// </summary>
public interface ILocalChangeDetectionService : IDisposable
{
    /// <summary>
    /// Starts monitoring the specified directory for file system changes.
    /// </summary>
    /// <param name="path">The directory path to monitor.</param>
    /// <exception cref="ArgumentException">Thrown when path is null, empty, or whitespace.</exception>
    void StartWatching(string path);

    /// <summary>
    /// Stops monitoring for file system changes and cleans up resources.
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Retrieves all pending changes that have been detected.
    /// </summary>
    /// <returns>A read-only list of detected changes.</returns>
    IReadOnlyList<LocalChange> GetPendingChanges();

    /// <summary>
    /// Clears all pending changes from the tracking collection.
    /// </summary>
    void ClearPendingChanges();
}
