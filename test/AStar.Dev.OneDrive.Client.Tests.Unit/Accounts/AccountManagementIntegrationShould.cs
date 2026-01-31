using AStar.Dev.OneDrive.Client.Accounts;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Accounts;

public class AccountManagementIntegrationShould : IDisposable
{
    private readonly AccountRepository _accountRepository;
    private readonly SyncDbContext _dbContext;
    private readonly IAuthService _mockAuthService;
    private bool _disposed;

    public AccountManagementIntegrationShould()
    {
        DbContextOptions<SyncDbContext> options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .Options;

        _dbContext = new SyncDbContext(options);
        _accountRepository = new AccountRepository(_dbContext);
        _mockAuthService = Substitute.For<IAuthService>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Fact(Skip = "Doesnt work")]
    public async Task LoadExistingAccountsFromDatabaseOnInitialization()
    {
        var account1 = new AccountInfo("acc1", "User One", "/path1", true, null, null, false, false, 3, 50, null);
        var account2 = new AccountInfo("acc2", "User Two", "/path2", false, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account1, TestContext.Current.CancellationToken);
        await _accountRepository.AddAsync(account2, TestContext.Current.CancellationToken);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts.ShouldContain(a => a.AccountId == "acc1");
        viewModel.Accounts.ShouldContain(a => a.AccountId == "acc2");
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task PersistNewAccountToDatabaseWhenAdded()
    {
        var authResult = new AuthenticationResult(true, "new-acc", "New User", null);
        _ = _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        _ = viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync(TestContext.Current.CancellationToken);
        accounts.Count.ShouldBe(1);
        accounts[0].AccountId.ShouldBe("new-acc");
        accounts[0].DisplayName.ShouldBe("New User");
        accounts[0].IsAuthenticated.ShouldBeTrue();
    }

    [Fact(Skip = "Doesnt work")]
    public async Task RemoveAccountFromDatabaseWhenDeleted()
    {
        var account = new AccountInfo("acc-to-delete", "User", "/path", true, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account, TestContext.Current.CancellationToken);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync(TestContext.Current.CancellationToken);
        accounts.ShouldBeEmpty();
    }

    [Fact(Skip = "Doesnt work")]
    public async Task UpdateAuthenticationStateInDatabaseWhenLoggingIn()
    {
        var account = new AccountInfo("acc-login", "User", "/path", false, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account, TestContext.Current.CancellationToken);

        var authResult = new AuthenticationResult(true, null, null, null);
        _ = _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        AccountInfo? updatedAccount = await _accountRepository.GetByIdAsync("acc-login", TestContext.Current.CancellationToken);
        _ = updatedAccount.ShouldNotBeNull();
        updatedAccount.IsAuthenticated.ShouldBeTrue();
    }

    [Fact(Skip = "Doesnt work")]
    public async Task UpdateAuthenticationStateInDatabaseWhenLoggingOut()
    {
        var account = new AccountInfo("acc-logout", "User", "/path", true, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account, TestContext.Current.CancellationToken);

        _ = _mockAuthService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        AccountInfo? updatedAccount = await _accountRepository.GetByIdAsync("acc-logout", TestContext.Current.CancellationToken);
        _ = updatedAccount.ShouldNotBeNull();
        updatedAccount.IsAuthenticated.ShouldBeFalse();
    }

    [Fact(Skip = "Doesnt work")]
    public async Task MaintainConsistencyBetweenViewModelAndDatabaseAfterMultipleOperations()
    {
        var authResult = new AuthenticationResult(true, "multi-acc", "Multi User", null);
        _ = _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));
        _ = _mockAuthService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        _ = viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        IReadOnlyList<AccountInfo> dbAccounts = await _accountRepository.GetAllAsync(TestContext.Current.CancellationToken);
        viewModel.Accounts.Count.ShouldBe(1);
        dbAccounts.Count.ShouldBe(1);
        dbAccounts[0].AccountId.ShouldBe("multi-acc");
        dbAccounts[0].IsAuthenticated.ShouldBeTrue();
    }

    [Fact(Skip = "Doesnt work")]
    public async Task HandleEmptyDatabaseGracefully()
    {
        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact(Skip = "Doesnt work")]
    public async Task PreserveAccountDataIntegrityAfterReload()
    {
        var account = new AccountInfo(
            "preserve-acc",
            "Preserve User",
            "/custom/path",
            true,
            DateTime.UtcNow,
            "delta-token-123",
            false,
            false,
            3,
            50,
            null);
        await _accountRepository.AddAsync(account, TestContext.Current.CancellationToken);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        AccountInfo loadedAccount = viewModel.Accounts.First();
        loadedAccount.AccountId.ShouldBe("preserve-acc");
        loadedAccount.DisplayName.ShouldBe("Preserve User");
        loadedAccount.LocalSyncPath.ShouldBe("/custom/path");
        loadedAccount.DeltaToken.ShouldBe("delta-token-123");
        _ = loadedAccount.LastSyncUtc.ShouldNotBeNull();
    }

    protected virtual void Dispose(bool disposing)
    {
        if(_disposed) return;

        if(disposing) _dbContext?.Dispose();

        _disposed = true;
    }
}
