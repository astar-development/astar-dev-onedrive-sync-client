using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Services;

public class AccountCreationServiceShould
{
    private readonly IGraphApiClient _graphApiClient = Substitute.For<IGraphApiClient>();
    private readonly IHashingService _hashingService = Substitute.For<IHashingService>();
    private readonly ISecureTokenStorage _secureTokenStorage = Substitute.For<ISecureTokenStorage>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILogger<AccountCreationService> _logger = Substitute.For<ILogger<AccountCreationService>>();

    private readonly IAccountCreationService _accountCreationService;

    public AccountCreationServiceShould() => _accountCreationService = new AccountCreationService(
            _graphApiClient,
            _hashingService,
            _secureTokenStorage,
            _accountRepository,
            _logger);

    [Fact]
    public async Task CreateAccountSuccessfullyWithValidAuthToken()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));
        var userProfile = new UserProfile("user@example.com", "microsoft-account-id-789");
        const string hashedEmail = "hashed-email-value";
        const string hashedAccountId = "hashed-account-id-value";

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(userProfile));
        _hashingService.HashEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(hashedEmail));
        _hashingService.HashAccountIdAsync(Arg.Any<string>(), Arg.Any<long>()).Returns(Task.FromResult(hashedAccountId));
        _accountRepository.GetByHashedEmailAsync(Arg.Any<string>()).Returns(Task.FromResult((Account?)null));
        _secureTokenStorage.StoreTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        _accountRepository.CreateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Ok>();
        var okResult = result as Result<Account, AccountCreationError>.Ok;
        okResult!.Value.HashedEmail.ShouldBe(hashedEmail);
        okResult.Value.HashedAccountId.ShouldBe(hashedAccountId);
    }

    [Fact]
    public async Task ReturnValidationErrorWhenAuthTokenIsNull()
    {
        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(null!);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }

    [Fact]
    public async Task ReturnGraphApiErrorWhenUserProfileNull()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((UserProfile)null!));

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }

    [Fact]
    public async Task ReturnGraphApiErrorWhenGraphClientThrowsHttpRequestException()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserProfile>(new HttpRequestException("Network error")));

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }

    [Fact]
    public async Task ReturnAccountAlreadyExistsErrorWhenEmailHashExists()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));
        var userProfile = new UserProfile("user@example.com", "account-id-456");
        const string hashedEmail = "hashed-email";

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(userProfile));
        _hashingService.HashEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(hashedEmail));
        var existingAccount = new Account { Id = Guid.NewGuid(), HashedEmail = hashedEmail, HashedAccountId = "existing-id" };
        _accountRepository.GetByHashedEmailAsync(Arg.Any<string>()).Returns(Task.FromResult<Account?>(existingAccount));

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }

    [Fact]
    public async Task ReturnTokenStorageErrorWhenSecureStorageThrowsException()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));
        var userProfile = new UserProfile("user@example.com", "account-id-789");

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(userProfile));
        _hashingService.HashEmailAsync(Arg.Any<string>()).Returns(Task.FromResult("hashed-email"));
        _hashingService.HashAccountIdAsync(Arg.Any<string>(), Arg.Any<long>()).Returns(Task.FromResult("hashed-account-id"));
        _accountRepository.GetByHashedEmailAsync(Arg.Any<string>()).Returns(Task.FromResult((Account?)null));
        _secureTokenStorage.StoreTokenAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException<UserProfile>(new InvalidOperationException("Storage error")));

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }

    [Fact]
    public async Task ReturnRepositoryErrorWhenAccountCreateThrowsDbUpdateException()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));
        var userProfile = new UserProfile("user@example.com", "account-id-789");

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(userProfile));
        _hashingService.HashEmailAsync(Arg.Any<string>()).Returns(Task.FromResult("hashed-email"));
        _hashingService.HashAccountIdAsync(Arg.Any<string>(), Arg.Any<long>()).Returns(Task.FromResult("hashed-account-id"));
        _accountRepository.GetByHashedEmailAsync(Arg.Any<string>()).Returns(Task.FromResult((Account?)null));
        _secureTokenStorage.StoreTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        _accountRepository.CreateAsync(Arg.Any<Account>())
            .Returns(Task.FromException<UserProfile>(new Microsoft.EntityFrameworkCore.DbUpdateException("Constraint violation")));

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }

    [Fact]
    public async Task ReturnUnexpectedErrorForGeneralExceptions()
    {
        var authToken = new AuthToken("access_token_123", DateTime.UtcNow.AddHours(1));

        _graphApiClient.GetUserProfileAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserProfile>(new InvalidOperationException("Storage error")));

        Result<Account, AccountCreationError> result = await _accountCreationService.CreateAccountAsync(authToken);

        result.ShouldBeOfType<Result<Account, AccountCreationError>.Error>();
    }
}
