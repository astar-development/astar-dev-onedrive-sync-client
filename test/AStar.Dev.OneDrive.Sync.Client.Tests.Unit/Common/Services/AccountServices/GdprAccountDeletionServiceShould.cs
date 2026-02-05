using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Common.Services.AccountServices;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Common.Services.AccountServices;

public class GdprAccountDeletionServiceShould
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISecureTokenStorage _secureTokenStorage;
    private readonly ILogger<GdprAccountDeletionService> _logger;
    private readonly GdprAccountDeletionService _service;

    public GdprAccountDeletionServiceShould()
    {
        _accountRepository = Substitute.For<IAccountRepository>();
        _secureTokenStorage = Substitute.For<ISecureTokenStorage>();
        _logger = Substitute.For<ILogger<GdprAccountDeletionService>>();
        _service = new GdprAccountDeletionService(_accountRepository, _secureTokenStorage, _logger);
    }

    [Fact]
    public async Task DeleteAccountSuccessfullyWhenBothOperationsSucceed()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedAccountId = hashedAccountId,
            HashedEmail = "test@example.com"
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Returns(Task.CompletedTask);
        _secureTokenStorage.DeleteTokenAsync(hashedAccountId).Returns(Task.CompletedTask);

        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Ok>();
        var okResult = result as Result<bool, GdprAccountDeletionError>.Ok;
        okResult!.Value.ShouldBeTrue();

        await _accountRepository.Received(1).DeleteAsync(accountId);
        await _secureTokenStorage.Received(1).DeleteTokenAsync(hashedAccountId);
    }

    [Fact]
    public async Task ReturnAccountNotFoundWhenAccountDoesNotExist()
    {
        const string hashedAccountId = "nonexistent-hashed-id";

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(null));

        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Error>();
        var errorResult = result as Result<bool, GdprAccountDeletionError>.Error;
        errorResult!.Reason.ShouldBe(GdprAccountDeletionError.AccountNotFound);

        await _accountRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
        await _secureTokenStorage.DidNotReceive().DeleteTokenAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ReturnAccountNotFoundWhenHashedAccountIdIsNull()
    {
        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync(null!);

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Error>();
        var errorResult = result as Result<bool, GdprAccountDeletionError>.Error;
        errorResult!.Reason.ShouldBe(GdprAccountDeletionError.AccountNotFound);
    }

    [Fact]
    public async Task ReturnAccountNotFoundWhenHashedAccountIdIsEmpty()
    {
        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync(string.Empty);

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Error>();
        var errorResult = result as Result<bool, GdprAccountDeletionError>.Error;
        errorResult!.Reason.ShouldBe(GdprAccountDeletionError.AccountNotFound);
    }

    [Fact]
    public async Task ReturnAccountNotFoundWhenHashedAccountIdIsWhitespace()
    {
        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync("   ");

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Error>();
        var errorResult = result as Result<bool, GdprAccountDeletionError>.Error;
        errorResult!.Reason.ShouldBe(GdprAccountDeletionError.AccountNotFound);
    }

    [Fact]
    public async Task ReturnRepositoryErrorWhenAccountDeletionThrowsException()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedAccountId = hashedAccountId,
            HashedEmail = "test@example.com"
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Throws(new InvalidOperationException("Database error"));

        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Error>();
        var errorResult = result as Result<bool, GdprAccountDeletionError>.Error;
        errorResult!.Reason.ShouldBe(GdprAccountDeletionError.RepositoryError);

        await _secureTokenStorage.DidNotReceive().DeleteTokenAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ReturnPartialDeletionWhenTokenDeletionFailsAfterAccountDeleted()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedAccountId = hashedAccountId,
            HashedEmail = "test@example.com"
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Returns(Task.CompletedTask);
        _secureTokenStorage.DeleteTokenAsync(hashedAccountId).Throws(new InvalidOperationException("Token deletion failed"));

        Result<bool, GdprAccountDeletionError> result = await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        result.ShouldBeOfType<Result<bool, GdprAccountDeletionError>.Error>();
        var errorResult = result as Result<bool, GdprAccountDeletionError>.Error;
        errorResult!.Reason.ShouldBe(GdprAccountDeletionError.PartialDeletion);

        await _accountRepository.Received(1).DeleteAsync(accountId);
        await _secureTokenStorage.Received(1).DeleteTokenAsync(hashedAccountId);
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenAccountRepositoryIsNull()
    {
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new GdprAccountDeletionService(null!, _secureTokenStorage, _logger));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenSecureTokenStorageIsNull()
    {
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new GdprAccountDeletionService(_accountRepository, null!, _logger));

        exception.ParamName.ShouldBe("secureTokenStorage");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenLoggerIsNull()
    {
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new GdprAccountDeletionService(_accountRepository, _secureTokenStorage, null!));

        exception.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task LogInformationWhenDeletionStartsAndCompletes()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedAccountId = hashedAccountId,
            HashedEmail = "test@example.com"
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Returns(Task.CompletedTask);
        _secureTokenStorage.DeleteTokenAsync(hashedAccountId).Returns(Task.CompletedTask);

        await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        _logger.Received().LogInformation(Arg.Is<string>(s => s.Contains("Beginning GDPR-compliant deletion")), hashedAccountId);
        _logger.Received().LogInformation(Arg.Is<string>(s => s.Contains("GDPR-compliant deletion completed successfully")), hashedAccountId);
    }

    [Fact]
    public async Task LogWarningWhenAccountNotFound()
    {
        const string hashedAccountId = "nonexistent-hashed-id";

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(null));

        await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        _logger.Received().LogWarning(Arg.Is<string>(s => s.Contains("Account not found")), hashedAccountId);
    }

    [Fact]
    public async Task LogErrorWhenAccountDeletionFails()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedAccountId = hashedAccountId,
            HashedEmail = "test@example.com"
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Throws(new InvalidOperationException("Database error"));

        await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        _logger.Received().LogError(Arg.Any<Exception>(), Arg.Is<string>(s => s.Contains("Error deleting account")), accountId);
    }

    [Fact]
    public async Task LogWarningWhenPartialDeletionOccurs()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedAccountId = hashedAccountId,
            HashedEmail = "test@example.com"
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Returns(Task.CompletedTask);
        _secureTokenStorage.DeleteTokenAsync(hashedAccountId).Throws(new InvalidOperationException("Token deletion failed"));

        await _service.DeleteAccountWithGdprComplianceAsync(hashedAccountId);

        _logger.Received().LogWarning(Arg.Is<string>(s => s.Contains("Partial deletion")), hashedAccountId);
    }
}
