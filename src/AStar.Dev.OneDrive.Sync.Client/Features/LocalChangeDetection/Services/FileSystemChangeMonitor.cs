using System.IO.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

/// <summary>
/// Implementation of IFileSystemChangeMonitor using FileSystemWatcher.
/// </summary>
public class FileSystemChangeMonitor(IFileSystem fileSystem) : IFileSystemChangeMonitor
{
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private IFileSystemWatcher? _watcher;
    private bool _isDisposed;

    public event EventHandler<FileSystemEventArgs>? FileCreated;
    public event EventHandler<FileSystemEventArgs>? FileModified;
    public event EventHandler<FileSystemEventArgs>? FileDeleted;
    public event EventHandler<RenamedEventArgs>? FileRenamed;

    public void StartMonitoring(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        StopMonitoring();

        _watcher = _fileSystem.FileSystemWatcher.New(path);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;

        _watcher.Created += (sender, e) => FileCreated?.Invoke(sender, e);
        _watcher.Changed += (sender, e) => FileModified?.Invoke(sender, e);
        _watcher.Deleted += (sender, e) => FileDeleted?.Invoke(sender, e);
        _watcher.Renamed += (sender, e) => FileRenamed?.Invoke(sender, e);
    }

    public void StopMonitoring()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        StopMonitoring();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
