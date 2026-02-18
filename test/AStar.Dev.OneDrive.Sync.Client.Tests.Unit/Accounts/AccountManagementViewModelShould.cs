using System.Reactive.Linq;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class AccountManagementViewModelShould
{
    private readonly IAuthService _authService;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountManagementViewModel> _logger;

    public AccountManagementViewModelShould()
    {
        _authService = Substitute.For<IAuthService>();
        _accountRepository = Substitute.For<IAccountRepository>();
        _logger = Substitute.For<ILogger<AccountManagementViewModel>>();

        _ = _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AuthenticationResult(false, string.Empty, new HashedAccountId(string.Empty), string.Empty, "Not configured")));
    }

    [Fact]
    public async Task InitializeWithEmptyAccountsList()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.ShouldBeEmpty();
        sut.SelectedAccount.ShouldBeNull();
        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadAccountsFromRepositoryOnInitialization()
    {
        var account1 = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user1@example.com", "/path1");
        var account2 = AccountInfo.Standard("id2", new HashedAccountId(AccountIdHasher.Hash("hash2")), "user2@example.com", "/path2");
        var accounts = new List<AccountInfo> { account1, account2 };
        _ = _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>(accounts));

        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        await Task.Delay(150, TestContext.Current.CancellationToken);

        sut.Accounts.Count.ShouldBe(2);
        sut.Accounts.ShouldContain(account1);
        sut.Accounts.ShouldContain(account2);
    }

    [Fact]
    public async Task SetIsLoadingTrueWhileLoadingAccounts()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<AccountInfo>>();
        _ = _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        sut.IsLoading.ShouldBeTrue();
        tcs.SetResult([]);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task AddAccountSuccessfully()
    {
        var authResult = new AuthenticationResult(Success: true,AccountId: "account123",HashedAccountId: new HashedAccountId(AccountIdHasher.Hash("hash123")),DisplayName: "test@example.com",ErrorMessage: null
        );
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        _ = sut.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.Count.ShouldBe(1);
        sut.Accounts[0].DisplayName.ShouldBe("test@example.com");
        sut.SelectedAccount?.Id.ShouldBe("account123");
        await _accountRepository.Received(1).AddAsync(Arg.Is<AccountInfo>(a => a.DisplayName == "test@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLocalSyncPathWithSanitizedDisplayName()
    {
        var authResult = new AuthenticationResult(Success: true,AccountId: "account123",HashedAccountId: new HashedAccountId(AccountIdHasher.Hash("hash123")),DisplayName: "user@domain.com",ErrorMessage: null);
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        _ = sut.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        await _accountRepository.Received(1).AddAsync(
            Arg.Is<AccountInfo>(a => a.LocalSyncPath.Contains("user_domain_com")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ShowToastOnAddAccountFailure()
    {
        var authResult = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Authentication failed");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        _ = sut.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.ToastMessage.ShouldBe("Authentication failed");
        sut.ToastVisible.ShouldBeTrue();
        sut.Accounts.ShouldBeEmpty();
        await _accountRepository.DidNotReceive().AddAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveSelectedAccount()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        sut.Accounts.Add(account);
        sut.SelectedAccount = account;

        _ = sut.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.ShouldBeEmpty();
        sut.SelectedAccount.ShouldBeNull();
        await _accountRepository.Received(1).DeleteAsync("id1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotRemoveAccountWhenNoneSelected()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        sut.Accounts.Add(account);
        sut.SelectedAccount = null;

        _ = sut.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.Count.ShouldBe(1);
        await _accountRepository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginToSelectedAccount()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        sut.Accounts.Add(account);
        sut.SelectedAccount = account;
        var authResult = new AuthenticationResult(Success: true,AccountId: "id1",HashedAccountId: new HashedAccountId(AccountIdHasher.Hash("hash1")),DisplayName: "user@example.com",ErrorMessage: null);
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        _ = sut.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.SelectedAccount!.IsAuthenticated.ShouldBeTrue();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<AccountInfo>(a => a.Id == "id1" && a.IsAuthenticated), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowToastOnLoginFailure()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        sut.Accounts.Add(account);
        sut.SelectedAccount = account;
        var authResult = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Login failed");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        _ = sut.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.ToastMessage.ShouldBe("Login failed");
        sut.ToastVisible.ShouldBeTrue();
        await _accountRepository.DidNotReceive().UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutFromSelectedAccount()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        sut.Accounts.Add(account);
        sut.SelectedAccount = account;
        _ = _authService.LogoutAsync("id1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        _ = sut.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.SelectedAccount!.IsAuthenticated.ShouldBeFalse();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<AccountInfo>(a => a.Id == "id1" && !a.IsAuthenticated), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotUpdateAccountOnLogoutFailure()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        sut.Accounts.Add(account);
        sut.SelectedAccount = account;
        _ = _authService.LogoutAsync("id1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        _ = sut.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.SelectedAccount!.IsAuthenticated.ShouldBeTrue();
        await _accountRepository.DidNotReceive().UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DisableRemoveCommandWhenNoAccountSelected()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        sut.SelectedAccount = null;

        var canExecute = false;
        using IDisposable subscription = sut.RemoveAccountCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableRemoveCommandWhenAccountSelected()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        sut.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = sut.RemoveAccountCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public void DisableLoginCommandWhenNoAccountSelected()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        sut.SelectedAccount = null;

        var canExecute = false;
        using IDisposable subscription = sut.LoginCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableLoginCommandWhenAccountAlreadyAuthenticated()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        sut.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = sut.LoginCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableLoginCommandWhenAccountNotAuthenticated()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = false };
        sut.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = sut.LoginCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public void DisableLogoutCommandWhenNoAccountSelected()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        sut.SelectedAccount = null;

        var canExecute = false;
        using IDisposable subscription = sut.LogoutCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableLogoutCommandWhenAccountNotAuthenticated()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = false };
        sut.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = sut.LogoutCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableLogoutCommandWhenAccountAuthenticated()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        sut.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = sut.LogoutCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task HideToastAfterFiveSeconds()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var authResult = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Test error");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        _ = sut.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.ToastVisible.ShouldBeTrue();
        sut.ToastMessage.ShouldBe("Test error");
        await Task.Delay(5100, TestContext.Current.CancellationToken);
        sut.ToastVisible.ShouldBeFalse();
        sut.ToastMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CancelPreviousToastWhenShowingNewToast()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var authResult1 = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "First error");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult1));

        _ = sut.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.ToastMessage.ShouldBe("First error");
        var authResult2 = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Second error");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult2));
        _ = sut.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        sut.ToastMessage.ShouldBe("Second error");
    }

    [Fact]
    public void RaisePropertyChangedForToastMessage()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var propertyChanged = false;
        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.ToastMessage))
                propertyChanged = true;
        };

        sut.ToastMessage = "Test message";

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedForToastVisible()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var propertyChanged = false;
        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.ToastVisible))
                propertyChanged = true;
        };

        sut.ToastVisible = true;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedForSelectedAccount()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var propertyChanged = false;
        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.SelectedAccount))
                propertyChanged = true;
        };

        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedForIsLoading()
    {
        var sut = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        var propertyChanged = false;
        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.IsLoading))
                propertyChanged = true;
        };

        sut.IsLoading = true;

        propertyChanged.ShouldBeTrue();
    }
}
