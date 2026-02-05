using AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.LocalChangeDetection.Services;

public class LocalChangeDetectionServiceShould : IDisposable
{
    private IFileSystemChangeMonitor _monitor = null!;
    private LocalChangeDetectionService _service = null!;
    private EventHandler<FileSystemEventArgs>? _fileCreatedHandler;
    private EventHandler<FileSystemEventArgs>? _fileModifiedHandler;
    private EventHandler<FileSystemEventArgs>? _fileDeletedHandler;
    private EventHandler<RenamedEventArgs>? _fileRenamedHandler;

    private void SetupTest()
    {
        _monitor = Substitute.For<IFileSystemChangeMonitor>();
        
        _monitor.When(x => x.FileCreated += Arg.Any<EventHandler<FileSystemEventArgs>>())
            .Do(x => _fileCreatedHandler = x.Arg<EventHandler<FileSystemEventArgs>>());
        
        _monitor.When(x => x.FileCreated -= Arg.Any<EventHandler<FileSystemEventArgs>>())
            .Do(_ => _fileCreatedHandler = null);
        
        _monitor.When(x => x.FileModified += Arg.Any<EventHandler<FileSystemEventArgs>>())
            .Do(x => _fileModifiedHandler = x.Arg<EventHandler<FileSystemEventArgs>>());
        
        _monitor.When(x => x.FileModified -= Arg.Any<EventHandler<FileSystemEventArgs>>())
            .Do(_ => _fileModifiedHandler = null);
        
        _monitor.When(x => x.FileDeleted += Arg.Any<EventHandler<FileSystemEventArgs>>())
            .Do(x => _fileDeletedHandler = x.Arg<EventHandler<FileSystemEventArgs>>());
        
        _monitor.When(x => x.FileDeleted -= Arg.Any<EventHandler<FileSystemEventArgs>>())
            .Do(_ => _fileDeletedHandler = null);
        
        _monitor.When(x => x.FileRenamed += Arg.Any<EventHandler<RenamedEventArgs>>())
            .Do(x => _fileRenamedHandler = x.Arg<EventHandler<RenamedEventArgs>>());
        
        _monitor.When(x => x.FileRenamed -= Arg.Any<EventHandler<RenamedEventArgs>>())
            .Do(_ => _fileRenamedHandler = null);
        
        _service = new LocalChangeDetectionService(_monitor);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenPathIsNull()
    {
        SetupTest();
        Should.Throw<ArgumentException>(() => _service.StartWatching(null!));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenPathIsEmpty()
    {
        SetupTest();
        Should.Throw<ArgumentException>(() => _service.StartWatching(string.Empty));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenPathIsWhitespace()
    {
        SetupTest();
        Should.Throw<ArgumentException>(() => _service.StartWatching("   "));
    }

    [Fact]
    public void StartWatchingInitializesMonitorWithPath()
    {
        SetupTest();
        const string path = "/test/path";

        _service.StartWatching(path);

        _monitor.Received(1).StartMonitoring(path);
    }

    [Fact]
    public void StopWatchingStopsMonitor()
    {
        SetupTest();
        _service.StartWatching("/test/path");

        _service.StopWatching();

        _monitor.Received(1).StopMonitoring();
    }

    [Fact]
    public void DetectNewFileCreation()
    {
        SetupTest();
        _service.StartWatching("/test/path");
        RaiseFileCreatedEvent("/test/newfile.txt");
        Thread.Sleep(350);

        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();
        changes.ShouldHaveSingleItem();
        changes[0].ChangeType.ShouldBe(LocalChangeType.Added);
    }

    [Fact]
    public void DetectFileModification()
    {
        SetupTest();
        _service.StartWatching("/test/path");
        RaiseFileModifiedEvent("/test/file.txt");
        Thread.Sleep(350);

        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();
        changes.ShouldHaveSingleItem();
        changes[0].ChangeType.ShouldBe(LocalChangeType.Modified);
    }

    [Fact]
    public void DetectFileDeletion()
    {
        SetupTest();
        _service.StartWatching("/test/path");
        RaiseFileDeletedEvent("/test/file.txt");
        Thread.Sleep(350);

        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();
        changes.ShouldHaveSingleItem();
        changes[0].ChangeType.ShouldBe(LocalChangeType.Deleted);
    }

    [Fact]
    public void DetectFileRename()
    {
        SetupTest();
        const string newFilePath = "/test/newname.txt";
        const string oldFilePath = "/test/oldname.txt";
        
        _service.StartWatching("/test/path");
        RaiseFileRenamedEvent(newFilePath, oldFilePath);
        Thread.Sleep(350);

        var normalizedOldPath = oldFilePath.Replace('/', Path.DirectorySeparatorChar);
        var expectedOldFullPath = Path.Combine(Path.GetDirectoryName(normalizedOldPath)!, Path.GetFileName(normalizedOldPath));
        
        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();
        changes.ShouldHaveSingleItem();
        changes[0].ChangeType.ShouldBe(LocalChangeType.Renamed);
        changes[0].OldFilePath.ShouldBe(expectedOldFullPath);
    }

    [Fact]
    public void ReturnEmptyListWhenNoPendingChanges()
    {
        SetupTest();
        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();

        changes.ShouldBeEmpty();
    }

    [Fact]
    public void ClearPendingChangesRemovesAllChanges()
    {
        SetupTest();
        _service.StartWatching("/test/path");
        RaiseFileCreatedEvent("/test/file.txt");
        Thread.Sleep(350);

        _service.ClearPendingChanges();

        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();
        changes.ShouldBeEmpty();
    }

    [Fact]
    public void StopWatchingPreventsNewChangesFromBeingTracked()
    {
        SetupTest();
        _service.StartWatching("/test/path");
        _service.StopWatching();
        RaiseFileCreatedEvent("/test/file.txt");
        Thread.Sleep(350);

        IReadOnlyList<LocalChange> changes = _service.GetPendingChanges();
        changes.ShouldBeEmpty();
    }

    public void Dispose()
    {
        _service?.Dispose();
        _monitor?.Dispose();
    }

    private void RaiseFileCreatedEvent(string fullPath)
    {
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath));
        _fileCreatedHandler?.Invoke(null, args);
    }

    private void RaiseFileModifiedEvent(string fullPath)
    {
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath));
        _fileModifiedHandler?.Invoke(null, args);
    }

    private void RaiseFileDeletedEvent(string fullPath)
    {
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath));
        _fileDeletedHandler?.Invoke(null, args);
    }

    private void RaiseFileRenamedEvent(string fullPath, string oldFullPath)
    {
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        oldFullPath = oldFullPath.Replace('/', Path.DirectorySeparatorChar);
        var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath), Path.GetFileName(oldFullPath));
        _fileRenamedHandler?.Invoke(null, args);
    }
}
