namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

/// <summary>
/// Defines the contract for monitoring file system changes.
/// </summary>
public interface IFileSystemChangeMonitor : IDisposable
{
    /// <summary>
    /// Occurs when a file or directory is created.
    /// </summary>
    event EventHandler<FileSystemEventArgs>? FileCreated;

    /// <summary>
    /// Occurs when a file or directory is modified.
    /// </summary>
    event EventHandler<FileSystemEventArgs>? FileModified;

    /// <summary>
    /// Occurs when a file or directory is deleted.
    /// </summary>
    event EventHandler<FileSystemEventArgs>? FileDeleted;

    /// <summary>
    /// Occurs when a file or directory is renamed.
    /// </summary>
    event EventHandler<RenamedEventArgs>? FileRenamed;

    /// <summary>
    /// Starts monitoring the specified directory for file system changes.
    /// </summary>
    /// <param name="path">The directory path to monitor.</param>
    void StartMonitoring(string path);

    /// <summary>
    /// Stops monitoring for file system changes.
    /// </summary>
    void StopMonitoring();
}
