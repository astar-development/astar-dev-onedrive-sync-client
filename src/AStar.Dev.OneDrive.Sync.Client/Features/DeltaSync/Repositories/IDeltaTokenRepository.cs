using AStar.Dev.OneDrive.Sync.Client.Common.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Repositories;

/// <summary>
/// Repository interface for managing DeltaToken persistence operations.
/// </summary>
public interface IDeltaTokenRepository
{
    /// <summary>
    /// Retrieves the delta token for a specific account and drive.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <param name="driveName">The drive name (e.g., "root", "documents").</param>
    /// <returns>The delta token if found; otherwise, null.</returns>
    Task<DeltaToken?> GetByAccountAndDriveAsync(string hashedAccountId, string driveName);

    /// <summary>
    /// Retrieves all delta tokens for a specific account.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <returns>A collection of delta tokens for the account.</returns>
    Task<IEnumerable<DeltaToken>> GetAllByAccountAsync(string hashedAccountId);

    /// <summary>
    /// Saves or updates a delta token.
    /// </summary>
    /// <param name="deltaToken">The delta token to save or update.</param>
    Task SaveAsync(DeltaToken deltaToken);

    /// <summary>
    /// Deletes a delta token by its identifier.
    /// </summary>
    /// <param name="id">The delta token identifier.</param>
    Task DeleteAsync(string id);
}
