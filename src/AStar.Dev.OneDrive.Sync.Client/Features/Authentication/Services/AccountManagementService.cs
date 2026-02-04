using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Service for managing account lifecycle operations (retrieve, update, delete).
/// Handles account settings updates and simple deletion.
/// </summary>
/// <remarks>
/// This service provides CRUD operations for Account entities with functional error handling.
/// Full GDPR-compliant deletion (cascade delete + secure storage cleanup) is implemented
/// in a separate service (Task 2.7).
/// </remarks>
/// <param name="accountRepository">Repository for account persistence.</param>
/// <param name="logger">Logger for diagnostics and monitoring.</param>
public class AccountManagementService(IAccountRepository accountRepository, ILogger<AccountManagementService> logger) : IAccountManagementService
{
    private readonly IAccountRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    private readonly ILogger<AccountManagementService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<Result<Account, AccountManagementError>> GetAccountByIdAsync(string hashedAccountId)
    {
        try
        {
            _logger.LogInformation("Retrieving account with hashed ID {HashedAccountId}", hashedAccountId);

            Account? account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId);

            if(account is null)
            {
                _logger.LogWarning("Account with hashed ID {HashedAccountId} not found", hashedAccountId);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.AccountNotFound);
            }

            _logger.LogInformation("Account {HashedAccountId} retrieved successfully", hashedAccountId);

            return new Result<Account, AccountManagementError>.Ok(account);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.UnexpectedError);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Account, AccountManagementError>> UpdateHomeSyncDirectoryAsync(string hashedAccountId, string? homeSyncDirectory)
    {
        try
        {
            _logger.LogInformation("Updating home sync directory for account {HashedAccountId}", hashedAccountId);

            Account? account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId);

            if(account is null)
            {
                _logger.LogWarning("Account with hashed ID {HashedAccountId} not found", hashedAccountId);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.AccountNotFound);
            }

            account.HomeSyncDirectory = homeSyncDirectory;
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);

            _logger.LogInformation("Home sync directory updated for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Ok(account);
        }
        catch(DbUpdateException ex)
        {
            _logger.LogError(ex, "Repository error updating home sync directory for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.RepositoryError);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating home sync directory for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.UnexpectedError);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Account, AccountManagementError>> UpdateMaxConcurrentAsync(string hashedAccountId, int maxConcurrent)
    {
        try
        {
            if(maxConcurrent < 1)
            {
                _logger.LogWarning("Validation error: MaxConcurrent must be >= 1, received {MaxConcurrent}", maxConcurrent);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.ValidationError);
            }

            _logger.LogInformation("Updating MaxConcurrent to {MaxConcurrent} for account {HashedAccountId}", maxConcurrent, hashedAccountId);

            Account? account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId);

            if(account is null)
            {
                _logger.LogWarning("Account with hashed ID {HashedAccountId} not found", hashedAccountId);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.AccountNotFound);
            }

            account.MaxConcurrent = maxConcurrent;
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);

            _logger.LogInformation("MaxConcurrent updated for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Ok(account);
        }
        catch(DbUpdateException ex)
        {
            _logger.LogError(ex, "Repository error updating MaxConcurrent for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.RepositoryError);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating MaxConcurrent for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.UnexpectedError);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Account, AccountManagementError>> UpdateDebugLoggingAsync(string hashedAccountId, bool enabled)
    {
        try
        {
            _logger.LogInformation("Updating debug logging to {Enabled} for account {HashedAccountId}", enabled, hashedAccountId);

            Account? account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId);

            if(account is null)
            {
                _logger.LogWarning("Account with hashed ID {HashedAccountId} not found", hashedAccountId);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.AccountNotFound);
            }

            account.DebugLoggingEnabled = enabled;
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);

            _logger.LogInformation("Debug logging updated for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Ok(account);
        }
        catch(DbUpdateException ex)
        {
            _logger.LogError(ex, "Repository error updating debug logging for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.RepositoryError);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating debug logging for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.UnexpectedError);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Account, AccountManagementError>> UpdateMaxBandwidthKBpsAsync(string hashedAccountId, int? maxBandwidthKBps)
    {
        try
        {
            if(maxBandwidthKBps.HasValue && maxBandwidthKBps.Value < 0)
            {
                _logger.LogWarning("Validation error: MaxBandwidthKBps must be >= 0 or null, received {MaxBandwidthKBps}", maxBandwidthKBps);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.ValidationError);
            }

            _logger.LogInformation("Updating MaxBandwidthKBps to {MaxBandwidthKBps} for account {HashedAccountId}", maxBandwidthKBps, hashedAccountId);

            Account? account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId);

            if(account is null)
            {
                _logger.LogWarning("Account with hashed ID {HashedAccountId} not found", hashedAccountId);

                return new Result<Account, AccountManagementError>.Error(AccountManagementError.AccountNotFound);
            }

            account.MaxBandwidthKBps = maxBandwidthKBps;
            account.UpdatedAt = DateTime.UtcNow;

            await _accountRepository.UpdateAsync(account);

            _logger.LogInformation("MaxBandwidthKBps updated for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Ok(account);
        }
        catch(DbUpdateException ex)
        {
            _logger.LogError(ex, "Repository error updating MaxBandwidthKBps for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.RepositoryError);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating MaxBandwidthKBps for account {HashedAccountId}", hashedAccountId);

            return new Result<Account, AccountManagementError>.Error(AccountManagementError.UnexpectedError);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool, AccountManagementError>> DeleteAccountAsync(string hashedAccountId)
    {
        try
        {
            _logger.LogInformation("Deleting account {HashedAccountId}", hashedAccountId);

            Account? account = await _accountRepository.GetByHashedAccountIdAsync(hashedAccountId);

            if(account is null)
            {
                _logger.LogWarning("Account with hashed ID {HashedAccountId} not found", hashedAccountId);

                return new Result<bool, AccountManagementError>.Error(AccountManagementError.AccountNotFound);
            }

            await _accountRepository.DeleteAsync(account.Id);

            _logger.LogInformation("Account {HashedAccountId} deleted successfully", hashedAccountId);

            return new Result<bool, AccountManagementError>.Ok(true);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting account {HashedAccountId}", hashedAccountId);

            return new Result<bool, AccountManagementError>.Error(AccountManagementError.UnexpectedError);
        }
    }
}
