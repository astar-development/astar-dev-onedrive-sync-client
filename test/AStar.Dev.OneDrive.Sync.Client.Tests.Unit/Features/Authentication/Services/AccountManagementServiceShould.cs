using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

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
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id"
        };

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));

        Result<Account, AccountManagementError> result = await _accountManagementService.GetAccountByIdAsync(accountId);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.Id.ShouldBe(accountId);
    }

    [Fact]
    public async Task GetAccountByIdReturnsAccountNotFoundWhenNotExists()
    {
        var accountId = Guid.NewGuid();

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(null));

        Result<Account, AccountManagementError> result = await _accountManagementService.GetAccountByIdAsync(accountId);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.AccountNotFound);
    }

    [Fact]
    public async Task GetAccountByIdReturnsRepositoryErrorWhenExceptionThrown()
    {
        var accountId = Guid.NewGuid();

        _accountRepository.GetByIdAsync(accountId).Throws(new InvalidOperationException("Database error"));

        Result<Account, AccountManagementError> result = await _accountManagementService.GetAccountByIdAsync(accountId);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.UnexpectedError);
    }

    [Fact]
    public async Task UpdateHomeSyncDirectoryUpdatesSuccessfully()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id",
            HomeSyncDirectory = "/old/path"
        };
        const string newDirectory = "/new/sync/path";

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateHomeSyncDirectoryAsync(accountId, newDirectory);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.HomeSyncDirectory.ShouldBe(newDirectory);
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.HomeSyncDirectory == newDirectory));
    }

    [Fact]
    public async Task UpdateHomeSyncDirectoryReturnsAccountNotFoundWhenNotExists()
    {
        var accountId = Guid.NewGuid();
        const string newDirectory = "/new/path";

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(null));

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateHomeSyncDirectoryAsync(accountId, newDirectory);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.AccountNotFound);
    }

    [Fact]
    public async Task UpdateMaxConcurrentUpdatesSuccessfully()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id",
            MaxConcurrent = 5
        };
        const int newMaxConcurrent = 10;

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxConcurrentAsync(accountId, newMaxConcurrent);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.MaxConcurrent.ShouldBe(newMaxConcurrent);
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.MaxConcurrent == newMaxConcurrent));
    }

    [Fact]
    public async Task UpdateMaxConcurrentReturnsValidationErrorForInvalidValue()
    {
        var accountId = Guid.NewGuid();
        const int invalidMaxConcurrent = 0;

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxConcurrentAsync(accountId, invalidMaxConcurrent);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.ValidationError);
    }

    [Fact]
    public async Task UpdateDebugLoggingUpdatesSuccessfully()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id",
            DebugLoggingEnabled = false
        };
        const bool newDebugLoggingEnabled = true;

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateDebugLoggingAsync(accountId, newDebugLoggingEnabled);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.DebugLoggingEnabled.ShouldBeTrue();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.DebugLoggingEnabled == true));
    }

    [Fact]
    public async Task UpdateMaxBandwidthKBpsUpdatesSuccessfully()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id",
            MaxBandwidthKBps = null
        };
        const int newMaxBandwidth = 1024;

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(accountId, newMaxBandwidth);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.MaxBandwidthKBps.ShouldBe(newMaxBandwidth);
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.MaxBandwidthKBps == newMaxBandwidth));
    }

    [Fact]
    public async Task UpdateMaxBandwidthKBpsAllowsNullForUnlimited()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id",
            MaxBandwidthKBps = 1024
        };

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(accountId, null);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Ok>();
        var okResult = result as Result<Account, AccountManagementError>.Ok;
        okResult!.Value.MaxBandwidthKBps.ShouldBeNull();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<Account>(a => a.MaxBandwidthKBps == null));
    }

    [Fact]
    public async Task UpdateMaxBandwidthKBpsReturnsValidationErrorForNegativeValue()
    {
        var accountId = Guid.NewGuid();
        const int invalidBandwidth = -100;

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(accountId, invalidBandwidth);

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.ValidationError);
    }

    [Fact]
    public async Task DeleteAccountDeletesSuccessfully()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id"
        };

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Returns(Task.CompletedTask);

        Result<bool, AccountManagementError> result = await _accountManagementService.DeleteAccountAsync(accountId);

        result.ShouldBeOfType<Result<bool, AccountManagementError>.Ok>();
        var okResult = result as Result<bool, AccountManagementError>.Ok;
        okResult!.Value.ShouldBeTrue();
        await _accountRepository.Received(1).DeleteAsync(accountId);
    }

    [Fact]
    public async Task DeleteAccountReturnsAccountNotFoundWhenNotExists()
    {
        var accountId = Guid.NewGuid();

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(null));

        Result<bool, AccountManagementError> result = await _accountManagementService.DeleteAccountAsync(accountId);

        result.ShouldBeOfType<Result<bool, AccountManagementError>.Error>();
        var errorResult = result as Result<bool, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.AccountNotFound);
    }

    [Fact]
    public async Task UpdateReturnsRepositoryErrorWhenDbUpdateExceptionThrown()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id"
        };

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.UpdateAsync(Arg.Any<Account>()).Throws(new DbUpdateException("Database error"));

        Result<Account, AccountManagementError> result = await _accountManagementService.UpdateHomeSyncDirectoryAsync(accountId, "/new/path");

        result.ShouldBeOfType<Result<Account, AccountManagementError>.Error>();
        var errorResult = result as Result<Account, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.RepositoryError);
    }

    [Fact]
    public async Task DeleteReturnsRepositoryErrorWhenExceptionThrown()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            HashedEmail = "test@example.com",
            HashedAccountId = "hashed-id"
        };

        _accountRepository.GetByIdAsync(accountId).Returns(Task.FromResult<Account?>(account));
        _accountRepository.DeleteAsync(accountId).Throws(new InvalidOperationException("Database error"));

        Result<bool, AccountManagementError> result = await _accountManagementService.DeleteAccountAsync(accountId);

        result.ShouldBeOfType<Result<bool, AccountManagementError>.Error>();
        var errorResult = result as Result<bool, AccountManagementError>.Error;
        errorResult!.Reason.ShouldBe(AccountManagementError.UnexpectedError);
    }
}
