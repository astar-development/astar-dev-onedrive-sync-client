using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Common.Services.AccountServices;

/// <summary>
/// Service for GDPR-compliant account deletion with cascade delete and secure storage cleanup.
/// </summary>
public class GdprAccountDeletionService : IGdprAccountDeletionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISecureTokenStorage _secureTokenStorage;
    private readonly ILogger<GdprAccountDeletionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GdprAccountDeletionService"/> class.
    /// </summary>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="secureTokenStorage">The secure token storage.</param>
    /// <param name="logger">The logger instance.</param>
    public GdprAccountDeletionService(
        IAccountRepository accountRepository,
        ISecureTokenStorage secureTokenStorage,
        ILogger<GdprAccountDeletionService> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _secureTokenStorage = secureTokenStorage ?? throw new ArgumentNullException(nameof(secureTokenStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Deletes an account and all related data in a GDPR-compliant manner.
    /// CASCADE DELETE handles related entity deletion automatically (ApplicationLog, ConflictLog, DeltaToken, FileSystemItem).
    /// Continues on errors to provide complete deletion status.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <returns>
    /// Result containing true if deletion succeeded completely,
    /// or an error indicating what failed.
    /// </returns>
    public async Task<Result<bool, GdprAccountDeletionError>> DeleteAccountWithGdprComplianceAsync(string hashedAccountId)
    {
        if (string.IsNullOrWhiteSpace(hashedAccountId))
        {
            _logger.LogWarning("DeleteAccountWithGdprComplianceAsync called with null or empty hashedAccountId");
            return new Result<bool, GdprAccountDeletionError>.Error(GdprAccountDeletionError.AccountNotFound);
        }

        _logger.LogInformation("Beginning GDPR-compliant deletion for account with hashed ID: {HashedAccountId}", hashedAccountId);

        // 1. Retrieve account to get its internal ID
        var account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).ConfigureAwait(false);

        if (account is null)
        {
            _logger.LogWarning("Account not found for hashed ID: {HashedAccountId}", hashedAccountId);
            return new Result<bool, GdprAccountDeletionError>.Error(GdprAccountDeletionError.AccountNotFound);
        }

        var accountId = account.Id;

        _logger.LogInformation("Account found with internal ID: {AccountId}, proceeding with deletion", accountId);

        // Track deletion status
        bool accountDeleted = false;

        // 2. Delete account from repository (CASCADE DELETE handles related entities)
        try
        {
            await _accountRepository.DeleteAsync(accountId).ConfigureAwait(false);
            accountDeleted = true;
            _logger.LogInformation("Account deleted successfully: {AccountId}", accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account: {AccountId}", accountId);
            return new Result<bool, GdprAccountDeletionError>.Error(GdprAccountDeletionError.RepositoryError);
        }

        // 3. Delete secure token storage - continue even if account deletion succeeded
        try
        {
            await _secureTokenStorage.DeleteTokenAsync(hashedAccountId).ConfigureAwait(false);
            _logger.LogInformation("Secure token deleted successfully for: {HashedAccountId}", hashedAccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting secure token for: {HashedAccountId}", hashedAccountId);

            // If account was deleted but token cleanup failed, return partial deletion error
            if (accountDeleted)
            {
                _logger.LogWarning("Partial deletion: Account deleted but token cleanup failed for: {HashedAccountId}", hashedAccountId);
                return new Result<bool, GdprAccountDeletionError>.Error(GdprAccountDeletionError.PartialDeletion);
            }

            return new Result<bool, GdprAccountDeletionError>.Error(GdprAccountDeletionError.SecureStorageError);
        }

        // Both operations succeeded
        _logger.LogInformation("GDPR-compliant deletion completed successfully for: {HashedAccountId}", hashedAccountId);
        return new Result<bool, GdprAccountDeletionError>.Ok(true);
    }
}
