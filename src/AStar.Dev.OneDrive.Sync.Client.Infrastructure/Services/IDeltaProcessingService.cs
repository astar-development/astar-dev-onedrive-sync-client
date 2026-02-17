using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for managing delta token storage and processing delta queries from OneDrive.
/// </summary>
public interface IDeltaProcessingService
{
    /// <summary>
    ///     Retrieves the last saved delta token for the specified account.
    /// </summary>
    /// <param name="accountId">The account ID for which to retrieve the delta token.</param>
    /// <param name="hashedAccountId">The hashed account ID for which to retrieve the delta token.</param>  
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The delta token if it exists, otherwise null.</returns>
    Task<DeltaToken?> GetDeltaTokenAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken);

    /// <summary>
    ///     Saves or updates the delta token for the specified account.
    /// </summary>
    /// <param name="token">The delta token to save or update.</param>
    /// <param name="hashedAccountId">The hashed account ID for which to save the delta token.</param>  
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    Task SaveDeltaTokenAsync(DeltaToken token, HashedAccountId hashedAccountId, CancellationToken cancellationToken);

    /// <summary>
    ///     Processes all delta pages from OneDrive starting from the provided delta token.
    /// </summary>
    /// <param name="accountId">The account ID for which to process delta pages.</param>
    /// <param name="hashedAccountId">The hashed account ID for which to process delta pages.</param>
    /// <param name="deltaToken">The delta token to start from. If null, starts from the beginning.</param>
    /// <param name="progressCallback">Optional callback to report progress during processing.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>Tuple containing the final delta token, page count, and total items processed.</returns>
    Task<(DeltaToken finalToken, int pageCount, int totalItemsProcessed)> ProcessDeltaPagesAsync(
        string accountId,
        HashedAccountId hashedAccountId,
        DeltaToken? deltaToken,
        Action<SyncState>? progressCallback,
        CancellationToken cancellationToken);
}
