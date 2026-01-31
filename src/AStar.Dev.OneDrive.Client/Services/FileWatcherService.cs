using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Avoid string interpolation in logging - Acceptable for file watcher service

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
///     Service for monitoring local file system changes to trigger synchronization.
/// </summary>
/// <remarks>
///     Wraps FileSystemWatcher with debouncing (500ms) to handle rapid file changes
///     and partial writes. Supports monitoring multiple account directories independently.
/// </remarks>
public sealed class FileWatcherService(ILogger<FileWatcherService> logger) : IFileWatcherService
{
    private readonly Subject<FileChangeEvent> _fileChanges = new();
    private readonly Dictionary<string, WatcherContext> _watchers = [];
    private bool _disposed;

    /// <inheritdoc />
    public IObservable<FileChangeEvent> FileChanges => _fileChanges.AsObservable();

    /// <inheritdoc />
    public void StartWatching(string accountId, string localPath)
    {
        if(!Directory.Exists(localPath))
            throw new DirectoryNotFoundException($"Directory not found: {localPath}");

        if(_watchers.ContainsKey(accountId))
        {
            logger.LogWarning("Already watching path for account {AccountId}. Stopping existing watcher first.", accountId);
            StopWatching(accountId);
        }

        try
        {
            var watcher = new FileSystemWatcher(localPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.Size
                               | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            // Debounce buffer for Changed/Created events (500ms throttle)
            var changeBuffer = new Subject<FileSystemEventArgs>();
            IDisposable subscription = changeBuffer
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(e => ProcessFileChange(accountId, localPath, e));

            // Wire up FileSystemWatcher events
            watcher.Changed += (s, e) => changeBuffer.OnNext(e);
            watcher.Created += (s, e) => changeBuffer.OnNext(e);
            watcher.Deleted += (s, e) => EmitFileChange(accountId, localPath, e, FileChangeType.Deleted);
            watcher.Renamed += (s, e) => EmitFileChange(accountId, localPath, e, FileChangeType.Renamed);
            watcher.Error += (s, e) => HandleWatcherError(accountId, e);

            _watchers[accountId] = new WatcherContext(watcher, changeBuffer, subscription);
            logger.LogInformation("Started watching {Path} for account {AccountId}", localPath, accountId);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Failed to start watching {Path} for account {AccountId}", localPath, accountId);
            throw;
        }
    }

    /// <inheritdoc />
    public void StopWatching(string accountId)
    {
        if(_watchers.Remove(accountId, out WatcherContext? context))
        {
            context.Dispose();
            logger.LogInformation("Stopped watching for account {AccountId}", accountId);
        }
    }

    /// <summary>
    ///     Disposes all file system watchers and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if(_disposed)
            return;

        foreach(WatcherContext context in _watchers.Values)
            context.Dispose();

        _watchers.Clear();
        _fileChanges.Dispose();
        _disposed = true;

        logger.LogInformation("FileWatcherService disposed");
    }

    private void ProcessFileChange(string accountId, string basePath, FileSystemEventArgs e)
    {
        // Determine if this is a creation or modification
        FileChangeType changeType = e.ChangeType == WatcherChangeTypes.Created
            ? FileChangeType.Created
            : FileChangeType.Modified;

        EmitFileChange(accountId, basePath, e, changeType);
    }

    private void EmitFileChange(string accountId, string basePath, FileSystemEventArgs e, FileChangeType changeType)
    {
        try
        {
            var relativePath = Path.GetRelativePath(basePath, e.FullPath);

            var changeEvent = new FileChangeEvent(
                accountId,
                e.FullPath,
                relativePath,
                changeType,
                DateTime.UtcNow
            );

            _fileChanges.OnNext(changeEvent);
            logger.LogDebug("File change detected: {ChangeType} - {RelativePath} (Account: {AccountId})",
                changeType, relativePath, accountId);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error emitting file change event for {Path}", e.FullPath);
        }
    }

    private void HandleWatcherError(string accountId, ErrorEventArgs e)
    {
        Exception exception = e.GetException();
        logger.LogError(exception, "FileSystemWatcher error for account {AccountId}", accountId);

        // Optionally emit an error event or attempt to restart the watcher
        // For now, just log the error
    }

    /// <summary>
    ///     Context holding a FileSystemWatcher and its associated subscriptions.
    /// </summary>
    private sealed class WatcherContext(
        FileSystemWatcher watcher,
        Subject<FileSystemEventArgs> changeBuffer,
        IDisposable subscription)
        : IDisposable
    {
        public void Dispose()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            subscription.Dispose();
            changeBuffer.Dispose();
        }
    }
}
