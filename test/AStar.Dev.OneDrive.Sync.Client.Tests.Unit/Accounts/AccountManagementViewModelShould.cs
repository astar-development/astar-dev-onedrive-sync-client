using System.Reactive.Linq;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class AccountManagementViewModelShould : IDisposable
{
    private readonly IAuthService _authService;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountManagementViewModel> _logger;
    private AccountManagementViewModel _viewModel;

    public AccountManagementViewModelShould()
    {
        _authService = Substitute.For<IAuthService>();
        _accountRepository = Substitute.For<IAccountRepository>();
        _logger = Substitute.For<ILogger<AccountManagementViewModel>>();

        _ = _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(new AuthenticationResult(false, string.Empty, new HashedAccountId(string.Empty), string.Empty, "Not configured")));

        _viewModel = new AccountManagementViewModel(_authService, _accountRepository, _logger);
    }

    public void Dispose() => _viewModel.Dispose();

    [Fact]
    public async Task InitializeWithEmptyAccountsList()
    {
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.Accounts.ShouldBeEmpty();
        _viewModel.SelectedAccount.ShouldBeNull();
        _viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadAccountsFromRepositoryOnInitialization()
    {
        var account1 = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user1@example.com", "/path1");
        var account2 = AccountInfo.Standard("id2", new HashedAccountId(AccountIdHasher.Hash("hash2")), "user2@example.com", "/path2");
        var accounts = new List<AccountInfo> { account1, account2 };
        _ = _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>(accounts));
        _viewModel.Dispose();

        _viewModel = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        await Task.Delay(150, TestContext.Current.CancellationToken);

        _viewModel.Accounts.Count.ShouldBe(2);
        _viewModel.Accounts.ShouldContain(account1);
        _viewModel.Accounts.ShouldContain(account2);
    }

    [Fact]
    public async Task SetIsLoadingTrueWhileLoadingAccounts()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<AccountInfo>>();
        _ = _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

        _viewModel = new AccountManagementViewModel(_authService, _accountRepository, _logger);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        _viewModel.IsLoading.ShouldBeTrue();
        tcs.SetResult([]);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        _viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task AddAccountSuccessfully()
    {
        var authResult = new AuthenticationResult(Success: true,AccountId: "account123",HashedAccountId: new HashedAccountId(AccountIdHasher.Hash("hash123")),DisplayName: "test@example.com",ErrorMessage: null
        );
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult));

        _ = _viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.Accounts.Count.ShouldBe(1);
        _viewModel.Accounts[0].DisplayName.ShouldBe("test@example.com");
        _viewModel.SelectedAccount?.Id.ShouldBe("account123");
        await _accountRepository.Received(1).AddAsync(Arg.Is<AccountInfo>(a => a.DisplayName == "test@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLocalSyncPathWithSanitizedDisplayName()
    {
        var authResult = new AuthenticationResult(Success: true,AccountId: "account123",HashedAccountId: new HashedAccountId(AccountIdHasher.Hash("hash123")),DisplayName: "user@domain.com",ErrorMessage: null);
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult));

        _ = _viewModel.AddAccountCommand.Execute().Subscribe();
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
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult));

        _ = _viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.ToastMessage.ShouldBe("Authentication failed");
        _viewModel.ToastVisible.ShouldBeTrue();
        _viewModel.Accounts.ShouldBeEmpty();
        await _accountRepository.DidNotReceive().AddAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveSelectedAccount()
    {
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        _viewModel.Accounts.Add(account);
        _viewModel.SelectedAccount = account;

        _ = _viewModel.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.Accounts.ShouldBeEmpty();
        _viewModel.SelectedAccount.ShouldBeNull();
        await _accountRepository.Received(1).DeleteAsync("id1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotRemoveAccountWhenNoneSelected()
    {
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        _viewModel.Accounts.Add(account);
        _viewModel.SelectedAccount = null;

        _ = _viewModel.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.Accounts.Count.ShouldBe(1);
        await _accountRepository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginToSelectedAccount()
    {
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        _viewModel.Accounts.Add(account);
        _viewModel.SelectedAccount = account;
        var authResult = new AuthenticationResult(Success: true,AccountId: "id1",HashedAccountId: new HashedAccountId(AccountIdHasher.Hash("hash1")),DisplayName: "user@example.com",ErrorMessage: null);
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult));

        _ = _viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.SelectedAccount!.IsAuthenticated.ShouldBeTrue();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<AccountInfo>(a => a.Id == "id1" && a.IsAuthenticated), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowToastOnLoginFailure()
    {
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        _viewModel.Accounts.Add(account);
        _viewModel.SelectedAccount = account;
        var authResult = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Login failed");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult));

        _ = _viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.ToastMessage.ShouldBe("Login failed");
        _viewModel.ToastVisible.ShouldBeTrue();
        await _accountRepository.DidNotReceive().UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutFromSelectedAccount()
    {
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        _viewModel.Accounts.Add(account);
        _viewModel.SelectedAccount = account;
        _ = _authService.LogoutAsync("id1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<bool, ErrorResponse>>(true));

        _ = _viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.SelectedAccount!.IsAuthenticated.ShouldBeFalse();
        await _accountRepository.Received(1).UpdateAsync(Arg.Is<AccountInfo>(a => a.Id == "id1" && !a.IsAuthenticated), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotUpdateAccountOnLogoutFailure()
    {
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        _viewModel.Accounts.Add(account);
        _viewModel.SelectedAccount = account;
        _ = _authService.LogoutAsync("id1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<bool, ErrorResponse>>(false));

        _ = _viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.SelectedAccount!.IsAuthenticated.ShouldBeTrue();
        await _accountRepository.DidNotReceive().UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DisableRemoveCommandWhenNoAccountSelected()
    {
        _viewModel.SelectedAccount = null;

        var canExecute = false;
        using IDisposable subscription = _viewModel.RemoveAccountCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableRemoveCommandWhenAccountSelected()
    {
        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        _viewModel.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = _viewModel.RemoveAccountCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public void DisableLoginCommandWhenNoAccountSelected()
    {
        _viewModel.SelectedAccount = null;

        var canExecute = false;
        using IDisposable subscription = _viewModel.LoginCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableLoginCommandWhenAccountAlreadyAuthenticated()
    {
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        _viewModel.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = _viewModel.LoginCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableLoginCommandWhenAccountNotAuthenticated()
    {
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = false };
        _viewModel.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = _viewModel.LoginCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public void DisableLogoutCommandWhenNoAccountSelected()
    {
        _viewModel.SelectedAccount = null;

        var canExecute = false;
        using IDisposable subscription = _viewModel.LogoutCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableLogoutCommandWhenAccountNotAuthenticated()
    {
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = false };
        _viewModel.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = _viewModel.LogoutCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableLogoutCommandWhenAccountAuthenticated()
    {
        AccountInfo account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path") with { IsAuthenticated = true };
        _viewModel.SelectedAccount = account;

        var canExecute = false;
        using IDisposable subscription = _viewModel.LogoutCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task HideToastAfterFiveSeconds()
    {
        var authResult = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Test error");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult));

        _ = _viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.ToastVisible.ShouldBeTrue();
        _viewModel.ToastMessage.ShouldBe("Test error");
        await Task.Delay(5100, TestContext.Current.CancellationToken);
        _viewModel.ToastVisible.ShouldBeFalse();
        _viewModel.ToastMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CancelPreviousToastWhenShowingNewToast()
    {
        var authResult1 = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "First error");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult1));

        _ = _viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _viewModel.ToastMessage.ShouldBe("First error");
        var authResult2 = new AuthenticationResult(Success: false,AccountId: string.Empty,HashedAccountId: new HashedAccountId(string.Empty),DisplayName: string.Empty,ErrorMessage: "Second error");
        _ = _authService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<AuthenticationResult, ErrorResponse>>(authResult2));
        _ = _viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        _viewModel.ToastMessage.ShouldBe("Second error");
    }

    [Fact]
    public void RaisePropertyChangedForToastMessage()
    {
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.ToastMessage))
                propertyChanged = true;
        };

        _viewModel.ToastMessage = "Test message";

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedForToastVisible()
    {
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.ToastVisible))
                propertyChanged = true;
        };

        _viewModel.ToastVisible = true;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedForSelectedAccount()
    {
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.SelectedAccount))
                propertyChanged = true;
        };

        var account = AccountInfo.Standard("id1", new HashedAccountId(AccountIdHasher.Hash("hash1")), "user@example.com", "/path");
        _viewModel.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedForIsLoading()
    {
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.IsLoading))
                propertyChanged = true;
        };

        _viewModel.IsLoading = true;

        propertyChanged.ShouldBeTrue();
    }
}
