using AStar.Dev.Functional.Extensions;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;

namespace AStarOneDriveClient.Services;

public class SyncEngine2(ISyncConfigurationRepository syncConfigurationRepository) : ISyncEngine, IDisposable
{
    private bool _disposedValue;

    public IObservable<SyncState> Progress => throw new NotImplementedException();

    public Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<string>, ErrorResponse> selectedFolders = await syncConfigurationRepository.GetSelectedFolders2Async(accountId, cancellationToken);

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