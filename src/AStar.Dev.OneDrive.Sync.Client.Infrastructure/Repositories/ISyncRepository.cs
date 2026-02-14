using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

public interface ISyncRepository
{
    /// <summary>
    ///    Gets the last saved delta token for the specified account, if it exists.
    /// </summary>
    /// <param name="accountId">
    ///     The account ID for which to retrieve the delta token. This should be a hashed value to avoid storing personally identifiable information.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the delta token if it exists, otherwise null.
    /// </returns>
    Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken);

    /// <summary> 
    /// Saves or updates the delta token for the specified account.
    /// </summary>
    /// <param name="token">
    ///    The delta token to save or update. The AccountId property should be a hashed value to avoid storing personally identifiable information.
    /// </param>
    /// <param name="cancellationToken">
    ///   The cancellation token to monitor for cancellation requests.
    /// </param>
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Applies the provided list of drive items to the local data store for the specified account. This may involve inserting new items, updating existing items, or deleting items that are no longer present in OneDrive. The implementation should ensure that the local data store remains consistent with the state of OneDrive after this operation completes.
    /// </summary>
    /// <param name="accountId">
    ///     The account ID for which to apply the drive items. This should be a hashed value to avoid storing personally identifiable information.
    /// </param>
    /// <param name="items">The list of drive items to apply.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ApplyDriveItemsAsync(string accountId, IEnumerable<DriveItemEntity> items, CancellationToken cancellationToken);
}
