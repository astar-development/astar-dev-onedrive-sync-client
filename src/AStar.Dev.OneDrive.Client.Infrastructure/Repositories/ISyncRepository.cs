using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken);

    Task SaveOrUpdateDeltaTokenAsync(string accountId, DeltaToken token, CancellationToken cancellationToken);
    
    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(string accountId, IEnumerable<DriveItemRecord> items, CancellationToken cancellationToken);
}