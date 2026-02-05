using System.Collections.Concurrent;
using System.IO;
using System.Timers;
using AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

/// <summary>
/// Monitors local file system changes with debouncing to reduce duplicate change notifications.
/// </summary>
/// <param name="monitor">The file system change monitor.</param>
public class LocalChangeDetectionService(IFileSystemChangeMonitor monitor) : ILocalChangeDetectionService
{
    private readonly IFileSystemChangeMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    private readonly ConcurrentDictionary<string, LocalChange> _pendingChanges = new();
    private readonly ConcurrentDictionary<string, System.Timers.Timer> _debounceTimers = new();
    private readonly object _lock = new();
    private bool _isDisposed;
    private const int DebounceDelayMilliseconds = 300;

    public void StartWatching(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        lock (_lock)
        {
            _monitor.FileCreated += OnFileCreated;
            _monitor.FileModified += OnFileModified;
            _monitor.FileDeleted += OnFileDeleted;
            _monitor.FileRenamed += OnFileRenamed;
            _monitor.StartMonitoring(path);
        }
    }

    public void StopWatching()
    {
        lock (_lock)
        {
            _monitor.FileCreated -= OnFileCreated;
            _monitor.FileModified -= OnFileModified;
            _monitor.FileDeleted -= OnFileDeleted;
            _monitor.FileRenamed -= OnFileRenamed;
            _monitor.StopMonitoring();

            foreach (System.Timers.Timer timer in _debounceTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }

            _debounceTimers.Clear();
        }
    }

    public IReadOnlyList<LocalChange> GetPendingChanges() => _pendingChanges.Values.ToList().AsReadOnly();

    public void ClearPendingChanges() => _pendingChanges.Clear();

    private void OnFileCreated(object? sender, FileSystemEventArgs e) =>
        DebounceChange(e.FullPath, () => new LocalChange
        {
            FilePath = e.FullPath,
            ChangeType = LocalChangeType.Added
        });

    private void OnFileModified(object? sender, FileSystemEventArgs e) =>
        DebounceChange(e.FullPath, () =>
            _pendingChanges.TryGetValue(e.FullPath, out LocalChange? existing)
                ? existing
                : new LocalChange
                {
                    FilePath = e.FullPath,
                    ChangeType = LocalChangeType.Modified
                });

    private void OnFileDeleted(object? sender, FileSystemEventArgs e) =>
        DebounceChange(e.FullPath, () => new LocalChange
        {
            FilePath = e.FullPath,
            ChangeType = LocalChangeType.Deleted
        });

    private void OnFileRenamed(object? sender, RenamedEventArgs e)
    {
        if (_debounceTimers.TryRemove(e.OldFullPath, out System.Timers.Timer? oldTimer))
        {
            oldTimer.Stop();
            oldTimer.Dispose();
        }

        _ = _pendingChanges.TryRemove(e.OldFullPath, out _);

        DebounceChange(e.FullPath, () => new LocalChange
        {
            FilePath = e.FullPath,
            ChangeType = LocalChangeType.Renamed,
            OldFilePath = e.OldFullPath
        });
    }

    private void DebounceChange(string filePath, Func<LocalChange> changeFactory)
    {
        if (_debounceTimers.TryGetValue(filePath, out System.Timers.Timer? existingTimer))
        {
            existingTimer.Stop();
            existingTimer.Start();

            return;
        }

        var timer = new System.Timers.Timer(DebounceDelayMilliseconds)
        {
            AutoReset = false
        };

        timer.Elapsed += (_, _) =>
        {
            _ = _pendingChanges.AddOrUpdate(filePath, changeFactory(), (_, _) => changeFactory());

            if (_debounceTimers.TryRemove(filePath, out System.Timers.Timer? t))
            {
                t.Dispose();
            }
        };

        _debounceTimers[filePath] = timer;
        timer.Start();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        StopWatching();
        _monitor.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
