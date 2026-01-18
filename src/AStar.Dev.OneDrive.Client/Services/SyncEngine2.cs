using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Models;

namespace AStar.Dev.OneDrive.Client.Services;

public class SyncEngine2 : ISyncEngine, IDisposable
{
    private readonly List<(DateTime Timestamp, long Bytes)> _transferHistory = [];
    private readonly IRemoteChangeDetector remoteChangeDetector;
    private readonly ISyncConfigurationRepository syncConfigurationRepository;
    private bool _disposedValue;
    private long _lastCompletedBytes;
    private DateTime _lastProgressUpdate = DateTime.UtcNow;
    private BehaviorSubject<SyncState> _progressSubject;
    private CancellationTokenSource? _syncCancellation;

    public SyncEngine2(ISyncConfigurationRepository syncConfigurationRepository, IRemoteChangeDetector remoteChangeDetector)
    {
        this.syncConfigurationRepository = syncConfigurationRepository;
        this.remoteChangeDetector = remoteChangeDetector;
        _lastCompletedBytes = 0;
        _lastProgressUpdate = DateTime.UtcNow;

        var initialState = SyncState.CreateInitial(string.Empty);

        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IObservable<SyncState> Progress => _progressSubject;

    public Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        _lastCompletedBytes = 0; // to prevent unused variable warning
        _lastProgressUpdate = DateTime.UtcNow; // to prevent unused variable warning
        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var detectionSemaphore = new SemaphoreSlim(8, 8);
        await detectionSemaphore.WaitAsync(_syncCancellation!.Token);

        await DebugLog.EntryAsync("SyncEngine.StartSyncAsync", cancellationToken);
        var syncState = SyncState.CreateInitial(accountId);

        (var placeholder, _progressSubject) = ProgressReporterService.ReportProgress(syncState, _progressSubject, _lastProgressUpdate, _lastCompletedBytes, _transferHistory);

        await Task.Delay(5000, cancellationToken); // Simulate some work
        SyncState finalState = syncState with // this is just to simulate final state, currently the updates are not showing in the UI
        {
            CompletedBytes = 100,
            TotalBytes = 100,
            MegabytesPerSecond = 1.5,
            EstimatedSecondsRemaining = 0,
            LastUpdateUtc = DateTime.UtcNow
        };

        _ = ProgressReporterService.ReportProgress(finalState, _progressSubject, _lastProgressUpdate, _lastCompletedBytes, _transferHistory);
    }

    public Task StopSyncAsync() => throw new NotImplementedException();

    private async Task<(IReadOnlyList<FileMetadata>, string?)> CreateDetectionTasksAsync(string accountId, IList<string> folders, SemaphoreSlim detectionSemaphore)
    {
        await detectionSemaphore.WaitAsync(_syncCancellation!.Token);

        try
        {
            _syncCancellation!.Token.ThrowIfCancellationRequested();

            List<Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)>> detectTasks = [];
            foreach(var folderPath in folders) detectTasks.Add(remoteChangeDetector.DetectChangesAsync(accountId, folderPath, null, _syncCancellation?.Token ?? CancellationToken.None));

            (IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)[] changesPerFolder = await Task.WhenAll(detectTasks);
            List<FileMetadata> allChanges = [];
            string? latestDeltaLink = null;
            foreach((IReadOnlyList<FileMetadata>? changes, var newDeltaLink) in changesPerFolder)
            {
                if(newDeltaLink is not null) latestDeltaLink = newDeltaLink;

                if(changes is not null) allChanges.AddRange(changes);
            }

            return (allChanges, latestDeltaLink);
        }
        finally
        {
            _ = detectionSemaphore.Release();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposedValue)
        {
            if(disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _disposedValue = true;
        }
    }
}
