using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Services;

public class AccountManagementServiceShould
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILogger<AccountManagementService> _logger = Substitute.For<ILogger<AccountManagementService>>();

    private readonly IAccountManagementService _accountManagementService;

    public AccountManagementServiceShould() => _accountManagementService = new AccountManagementService(
            _accountRepository,
            _logger);

    [Fact]
    public async Task GetAccountByIdReturnsAccountWhenExists()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));

        Result<Account, AccountManagementError> result = await _accountManagementService.GetAccountByIdAsync(hashedAccountId);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.HashedAccountId.ShouldBe(hashedAccountId);
    }

    [Fact]
    public async Task GetAccountByIdReturnsAccountNotFoundWhenNotExists()
    {
        const string hashedAccountId = "nonexistent-hashed-id";

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(null));

        Result<Account, AccountManagementError> result = await _accountManagementService.GetAccountByIdAsync(hashedAccountId);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.AccountNotFound);
    }

    [Fact]
    public async Task GetAccountByIdReturnsRepositoryErrorWhenExceptionThrown()
    {
        const string hashedAccountId = "error-hashed-id";

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Throws(new InvalidOperationException("Database error"));

        Result<Account, AccountManagementError> result = await _accountManagementService.GetAccountByIdAsync(hashedAccountId);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.UnexpectedError);
    }

    [Fact]
    public async Task UpdateHomeSyncDirectoryUpdatesSuccessfully()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId,
            HomeSyncDirectory = "/old/path"
        };
        const string newDirectory = "/new/sync/path";

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateHomeSyncDirectoryAsync(hashedAccountId, newDirectory);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.HomeSyncDirectory.ShouldBe(newDirectory);
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.HomeSyncDirectory == newDirectory));
    }

    [Fact]
    public async Task UpdateHomeSyncDirectoryReturnsAccountNotFoundWhenNotExists()
    {
        const string hashedAccountId = "nonexistent-hashed-id";
        const string newDirectory = "/new/path";

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(null));

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateHomeSyncDirectoryAsync(hashedAccountId, newDirectory);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.AccountNotFound);
    }

    [Fact]
    public async Task UpdateMaxConcurrentUpdatesSuccessfully()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId,
            MaxConcurrent = 5
        };
        const int newMaxConcurrent = 10;

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxConcurrentAsync(hashedAccountId, newMaxConcurrent);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.MaxConcurrent.ShouldBe(newMaxConcurrent);
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.MaxConcurrent == newMaxConcurrent));
    }

    [Fact]
    public async Task UpdateMaxConcurrentReturnsValidationErrorForInvalidValue()
    {
        const string hashedAccountId = "hashed-account-id-test";
        const int invalidMaxConcurrent = 0;

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxConcurrentAsync(hashedAccountId, invalidMaxConcurrent);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.ValidationError);
    }

    [Fact]
    public async Task UpdateDebugLoggingUpdatesSuccessfully()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId,
            DebugLoggingEnabled = false
        };
        const bool newDebugLoggingEnabled = true;

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateDebugLoggingAsync(hashedAccountId, newDebugLoggingEnabled);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.DebugLoggingEnabled.ShouldBeTrue();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.DebugLoggingEnabled == true));
    }

    [Fact]
    public async Task UpdateMaxBandwidthKBpsUpdatesSuccessfully()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId,
            MaxBandwidthKBps = null
        };
        const int newMaxBandwidth = 1024;

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(hashedAccountId, newMaxBandwidth);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.MaxBandwidthKBps.ShouldBe(newMaxBandwidth);
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.MaxBandwidthKBps == newMaxBandwidth));
    }

    [Fact]
    public async Task UpdateMaxBandwidthKBpsAllowsNullForUnlimited()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId,
            MaxBandwidthKBps = 1024
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(hashedAccountId, null);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.MaxBandwidthKBps.ShouldBeNull();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.MaxBandwidthKBps == null));
    }

    [Fact]
    public async Task UpdateMaxBandwidthKBpsReturnsValidationErrorForNegativeValue()
    {
        const string hashedAccountId = "hashed-account-id-test";
        const int invalidBandwidth = -100;

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(hashedAccountId, invalidBandwidth);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.ValidationError);
    }

    [Fact]
    public async Task UpdateReturnsRepositoryErrorWhenDbUpdateExceptionThrown()
    {
        const string hashedAccountId = "hashed-account-id-test";
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "test@example.com",
            HashedAccountId = hashedAccountId
        };

        _accountRepository.GetByHashedAccountIdAsync(hashedAccountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Throws(new DbUpdateException("Database error"));

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateHomeSyncDirectoryAsync(hashedAccountId, "/new/path");

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.RepositoryError);
    }
}
