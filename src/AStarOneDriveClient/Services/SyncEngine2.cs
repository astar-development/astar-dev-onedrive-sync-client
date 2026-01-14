using System.Reactive.Subjects;
using AStar.Dev.Functional.Extensions;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;

namespace AStarOneDriveClient.Services;

public class SyncEngine2 : ISyncEngine, IDisposable
{
    public SyncEngine2(ISyncConfigurationRepository syncConfigurationRepository, IRemoteChangeDetector remoteChangeDetector)
    {
        this.syncConfigurationRepository = syncConfigurationRepository;
        this.remoteChangeDetector = remoteChangeDetector;
        _lastCompletedBytes = 0;
        _lastProgressUpdate = DateTime.UtcNow;

        var initialState = SyncState.CreateInitial(string.Empty);

        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }
    private bool _disposedValue;
    private CancellationTokenSource? _syncCancellation;
    private BehaviorSubject<SyncState> _progressSubject;
    private readonly ISyncConfigurationRepository syncConfigurationRepository;
    private readonly IRemoteChangeDetector remoteChangeDetector;
    private DateTime _lastProgressUpdate = DateTime.UtcNow;
    private long _lastCompletedBytes;
    private readonly List<(DateTime Timestamp, long Bytes)> _transferHistory = [];

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

        (IReadOnlyList<FileMetadata>, string) selectedFolders = await syncConfigurationRepository.GetSelectedFolders2Async(accountId, cancellationToken)
        .MatchAsync(
            async folders => await CreateDetectionTasksAsync(accountId, folders, detectionSemaphore),
            error => Task.FromResult<(IReadOnlyList<FileMetadata>, string?)>((Array.Empty<FileMetadata>(), error.Message))
        );
    }

    private async Task<(IReadOnlyList<FileMetadata>, string?)> CreateDetectionTasksAsync(string accountId, IList<string> folders, SemaphoreSlim detectionSemaphore)
    {
        await detectionSemaphore.WaitAsync(_syncCancellation!.Token);

        try
        {
            _syncCancellation!.Token.ThrowIfCancellationRequested();

            List<Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)>> detectTasks = [];
            foreach(var folderPath in folders)
            {
                detectTasks.Add(remoteChangeDetector.DetectChangesAsync(accountId, folderPath, null, _syncCancellation?.Token ?? CancellationToken.None));
            }

            (IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)[] changesPerFolder = await Task.WhenAll(detectTasks);
            List<FileMetadata> allChanges = [];
            string? latestDeltaLink = null;
            foreach((IReadOnlyList<FileMetadata>? changes, var newDeltaLink) in changesPerFolder)
            {
                if(newDeltaLink is not null)
                {
                    latestDeltaLink = newDeltaLink;
                }

                if(changes is not null)
                {
                    allChanges.AddRange(changes);
                }
            }

            return (allChanges, latestDeltaLink);
        }
        finally
        {
            _ = detectionSemaphore.Release();
        }
    }

    public Task StopSyncAsync() => throw new NotImplementedException();

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

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
