using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

/// <summary>
/// Integration tests for AccountManagementViewModel with real repository and in-memory database.
/// </summary>
public class AccountManagementIntegrationShould : IDisposable
{
    private readonly SyncDbContext _dbContext;
    private readonly AccountRepository _accountRepository;
    private readonly IAuthService _mockAuthService;
    private bool _disposed;

    public AccountManagementIntegrationShould()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SyncDbContext(options);
        _accountRepository = new AccountRepository(_dbContext);
        _mockAuthService = Substitute.For<IAuthService>();
    }

    [Fact]
    public async Task LoadExistingAccountsFromDatabaseOnInitialization()
    {
        var account1 = new AccountInfo("acc1", "User One", "/path1", true, null, null, false);
        var account2 = new AccountInfo("acc2", "User Two", "/path2", false, null, null, false);
        await _accountRepository.AddAsync(account1);
        await _accountRepository.AddAsync(account2);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(100);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts.ShouldContain(a => a.AccountId == "acc1");
        viewModel.Accounts.ShouldContain(a => a.AccountId == "acc2");
    }

    [Fact]
    public async Task PersistNewAccountToDatabaseWhenAdded()
    {
        var authResult = new AuthenticationResult(true, "new-acc", "New User", null);
        _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100);

        var accounts = await _accountRepository.GetAllAsync();
        accounts.Count.ShouldBe(1);
        accounts[0].AccountId.ShouldBe("new-acc");
        accounts[0].DisplayName.ShouldBe("New User");
        accounts[0].IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAccountFromDatabaseWhenDeleted()
    {
        var account = new AccountInfo("acc-to-delete", "User", "/path", true, null, null, false);
        await _accountRepository.AddAsync(account);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        viewModel.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100);

        var accounts = await _accountRepository.GetAllAsync();
        accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAuthenticationStateInDatabaseWhenLoggingIn()
    {
        var account = new AccountInfo("acc-login", "User", "/path", false, null, null, false);
        await _accountRepository.AddAsync(account);

        var authResult = new AuthenticationResult(true, null, null, null);
        _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100);

        var updatedAccount = await _accountRepository.GetByIdAsync("acc-login");
        updatedAccount.ShouldNotBeNull();
        updatedAccount.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAuthenticationStateInDatabaseWhenLoggingOut()
    {
        var account = new AccountInfo("acc-logout", "User", "/path", true, null, null, false);
        await _accountRepository.AddAsync(account);

        _mockAuthService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100);

        var updatedAccount = await _accountRepository.GetByIdAsync("acc-logout");
        updatedAccount.ShouldNotBeNull();
        updatedAccount.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public async Task MaintainConsistencyBetweenViewModelAndDatabaseAfterMultipleOperations()
    {
        var authResult = new AuthenticationResult(true, "multi-acc", "Multi User", null);
        _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));
        _mockAuthService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100);

        viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100);

        var dbAccounts = await _accountRepository.GetAllAsync();
        viewModel.Accounts.Count.ShouldBe(1);
        dbAccounts.Count.ShouldBe(1);
        dbAccounts[0].AccountId.ShouldBe("multi-acc");
        dbAccounts[0].IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleEmptyDatabaseGracefully()
    {
        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task PreserveAccountDataIntegrityAfterReload()
    {
        var account = new AccountInfo(
            "preserve-acc",
            "Preserve User",
            "/custom/path",
            true,
            DateTime.UtcNow,
            "delta-token-123",
            false);
        await _accountRepository.AddAsync(account);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50);

        var loadedAccount = viewModel.Accounts.First();
        loadedAccount.AccountId.ShouldBe("preserve-acc");
        loadedAccount.DisplayName.ShouldBe("Preserve User");
        loadedAccount.LocalSyncPath.ShouldBe("/custom/path");
        loadedAccount.DeltaToken.ShouldBe("delta-token-123");
        loadedAccount.LastSyncUtc.ShouldNotBeNull();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _dbContext?.Dispose();
        }

        _disposed = true;
    }
}
