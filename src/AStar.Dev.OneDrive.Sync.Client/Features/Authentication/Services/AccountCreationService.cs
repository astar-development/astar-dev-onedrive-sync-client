using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Orchestrates account creation after successful authentication.
/// Bridges authentication layer with persistence layer using functional error handling.
/// </summary>
/// <remarks>
/// This service handles the complete workflow:
/// 1. Retrieve user profile from Microsoft Graph API
/// 2. Check for duplicate accounts (GDPR compliance)
/// 3. Hash sensitive identifiers
/// 4. Store authentication token securely
/// 5. Persist account record to database
/// </remarks>
/// <param name="graphApiClient">Client for Microsoft Graph API interactions.</param>
/// <param name="hashingService">Service for hashing email and account ID.</param>
/// <param name="secureTokenStorage">Platform-specific secure token storage.</param>
/// <param name="accountRepository">Repository for account persistence.</param>
/// <param name="logger">Logger for diagnostics and monitoring.</param>
public class AccountCreationService(IGraphApiClient graphApiClient, IHashingService hashingService, ISecureTokenStorage secureTokenStorage, IAccountRepository accountRepository,
    ILogger<AccountCreationService> logger) : IAccountCreationService
{
    private readonly IGraphApiClient _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
    private readonly IHashingService _hashingService = hashingService ?? throw new ArgumentNullException(nameof(hashingService));
    private readonly ISecureTokenStorage _secureTokenStorage = secureTokenStorage ?? throw new ArgumentNullException(nameof(secureTokenStorage));
    private readonly IAccountRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    private readonly ILogger<AccountCreationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<Result<Account, AccountCreationError>> CreateAccountAsync(AuthToken authToken, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(authToken);

            _logger.LogInformation("Starting account creation process");

            UserProfile userProfile = await GetUserProfileAsync(authToken, cancellationToken);

            if(userProfile is null)
            {
                _logger.LogWarning("Graph API returned null user profile");
                
                return new Result<Account, AccountCreationError>.Error(AccountCreationError.GraphApiError);
            }

            var hashedEmail = await _hashingService.HashEmailAsync(userProfile.Email);
            var createdAtTicks = DateTime.UtcNow.Ticks;
            var hashedAccountId = await _hashingService.HashAccountIdAsync(userProfile.AccountId, createdAtTicks);

            Account? existingAccount = await _accountRepository.GetByHashedEmailAsync(hashedEmail);
            if(existingAccount is not null)
            {
                _logger.LogWarning("Account with hashed email already exists");

                return new Result<Account, AccountCreationError>.Error(AccountCreationError.AccountAlreadyExists);
            }

            await _secureTokenStorage.StoreTokenAsync(hashedAccountId, authToken.AccessToken);

            var newAccount = new Account
            {
                Id = Guid.NewGuid(),
                HashedEmail = hashedEmail,
                HashedAccountId = hashedAccountId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _accountRepository.CreateAsync(newAccount);

            _logger.LogInformation("Account created successfully with ID {AccountId}", newAccount.Id);

            return new Result<Account, AccountCreationError>.Ok(newAccount);
        }
        catch(ArgumentNullException)
        {
            _logger.LogWarning("Validation error: null authentication token");

            return new Result<Account, AccountCreationError>.Error(AccountCreationError.ValidationError);
        }
        catch(HttpRequestException ex)
        {
            _logger.LogError(ex, "Graph API error during account creation");

            return new Result<Account, AccountCreationError>.Error(AccountCreationError.GraphApiError);
        }
        catch(InvalidOperationException ex)
        {
            _logger.LogError(ex, "Secure storage error during account creation");

            return new Result<Account, AccountCreationError>.Error(AccountCreationError.TokenStorageError);
        }
        catch(DbUpdateException ex)
        {
            _logger.LogError(ex, "Repository error during account creation");

            return new Result<Account, AccountCreationError>.Error(AccountCreationError.RepositoryError);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during account creation");

            return new Result<Account, AccountCreationError>.Error(AccountCreationError.UnexpectedError);
        }
    }

    private async Task<UserProfile> GetUserProfileAsync(AuthToken authToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authToken);

        return await _graphApiClient.GetUserProfileAsync(authToken, cancellationToken);
    }
}
