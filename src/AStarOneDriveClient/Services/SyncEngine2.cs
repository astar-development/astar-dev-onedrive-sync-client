using AStar.Dev.Functional.Extensions;
using AStar.Dev.Utilities;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using System.Linq;

namespace AStarOneDriveClient.Services;

public class SyncEngine2(ISyncConfigurationRepository syncConfigurationRepository, IRemoteChangeDetector remoteChangeDetector) : ISyncEngine, IDisposable
{
    private bool _disposedValue;
    private CancellationTokenSource? _syncCancellation;

    public IObservable<SyncState> Progress => throw new NotImplementedException();

    public Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Result<IList<string>, ErrorResponse> selectedFolders = await syncConfigurationRepository.GetSelectedFolders2Async(accountId, cancellationToken);
        (IReadOnlyList<FileMetadata> Changes, string? Error)? result = await selectedFolders.MatchAsync(
            async folders =>
            {
                List<Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)>> detectTasks = new();
                foreach (var folderPath in folders)
                {
                    detectTasks.Add(remoteChangeDetector.DetectChangesAsync(accountId, folderPath, null, _syncCancellation.Token));
                }

                (IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)[] changesPerFolder = await Task.WhenAll(detectTasks);
                List<FileMetadata> allChanges = new();
                foreach ((IReadOnlyList<FileMetadata>? changes, var newDeltaLink) in changesPerFolder)
                {
                    if (changes is not null)
                    {
                        allChanges.AddRange(changes);
                    }
                }

                return (allChanges, null);
            },
            error => Task.FromResult<(IReadOnlyList<FileMetadata>, string?)>((Array.Empty<FileMetadata>(), error.Message))
        );

        throw new NotImplementedException();
    }

    public Task StopSyncAsync() => throw new NotImplementedException();

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
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