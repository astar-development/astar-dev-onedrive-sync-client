using AStarOneDriveClient.Models;

namespace AStarOneDriveClient.Repositories;

/// <summary>
/// Repository for managing sync state data.
/// </summary>
public interface ISyncStateRepository
{
    /// <summary>
    /// Gets the sync state for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync state if found, otherwise null.</returns>
    Task<SyncState?> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sync states.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all sync states.</returns>
    Task<IReadOnlyList<SyncState>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the sync state for an account.
    /// </summary>
    /// <param name="syncState">The sync state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(SyncState syncState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the sync state for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string accountId, CancellationToken cancellationToken = default);
}
