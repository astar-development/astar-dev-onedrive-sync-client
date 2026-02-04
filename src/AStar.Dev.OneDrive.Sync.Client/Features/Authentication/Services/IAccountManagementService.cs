using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Service for managing account lifecycle operations (retrieve, update, delete).
/// </summary>
public interface IAccountManagementService
{
    /// <summary>
    /// Retrieves an account by its ID.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <returns>
    /// Success: A Result containing the Account entity.
    /// Failure: A Result containing an AccountManagementError (AccountNotFound, RepositoryError, UnexpectedError).
    /// </returns>
    Task<Result<Account, AccountManagementError>> GetAccountByIdAsync(Guid accountId);

    /// <summary>
    /// Updates the home sync directory for an account.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <param name="homeSyncDirectory">The new home sync directory path. Can be null or empty.</param>
    /// <returns>
    /// Success: A Result containing the updated Account entity.
    /// Failure: A Result containing an AccountManagementError.
    /// </returns>
    Task<Result<Account, AccountManagementError>> UpdateHomeSyncDirectoryAsync(Guid accountId, string? homeSyncDirectory);

    /// <summary>
    /// Updates the maximum concurrent operations setting for an account.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <param name="maxConcurrent">The maximum number of concurrent operations (must be >= 1).</param>
    /// <returns>
    /// Success: A Result containing the updated Account entity.
    /// Failure: A Result containing an AccountManagementError.
    /// </returns>
    Task<Result<Account, AccountManagementError>> UpdateMaxConcurrentAsync(Guid accountId, int maxConcurrent);

    /// <summary>
    /// Updates the debug logging setting for an account.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <param name="enabled">True to enable debug logging; false to disable.</param>
    /// <returns>
    /// Success: A Result containing the updated Account entity.
    /// Failure: A Result containing an AccountManagementError.
    /// </returns>
    Task<Result<Account, AccountManagementError>> UpdateDebugLoggingAsync(Guid accountId, bool enabled);

    /// <summary>
    /// Updates the maximum bandwidth limit for an account.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <param name="maxBandwidthKBps">Maximum bandwidth in KB/s. Null for unlimited.</param>
    /// <returns>
    /// Success: A Result containing the updated Account entity.
    /// Failure: A Result containing an AccountManagementError.
    /// </returns>
    Task<Result<Account, AccountManagementError>> UpdateMaxBandwidthKBpsAsync(Guid accountId, int? maxBandwidthKBps);

    /// <summary>
    /// Deletes an account from the database.
    /// Note: This is a simple delete. Full GDPR compliance (cascade delete + secure storage cleanup)
    /// is implemented in Task 2.7.
    /// </summary>
    /// <param name="accountId">The unique account identifier.</param>
    /// <returns>
    /// Success: A Result containing true if deleted successfully.
    /// Failure: A Result containing an AccountManagementError.
    /// </returns>
    Task<Result<bool, AccountManagementError>> DeleteAccountAsync(Guid accountId);
}
