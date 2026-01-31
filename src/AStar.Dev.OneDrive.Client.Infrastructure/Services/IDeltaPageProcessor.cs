using AStar.Dev.OneDrive.Client.Core.Models;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Processes delta pages from the OneDrive Graph API and applies them to the local repository.
/// </summary>
public interface IDeltaPageProcessor
{
    /// <summary>
    ///     Processes all delta pages and reports progress via callback.
    /// </summary>
    /// <param name="deltaToken">The delta token to start from.</param>
    /// <param name="progressCallback">Callback to report progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>Tuple with final delta, page count, and total items processed.</returns>
    Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(string accountId, DeltaToken? deltaToken, Action<SyncState>? progressCallback,
        CancellationToken cancellationToken);
}
