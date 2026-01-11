using System.Reactive.Linq;
using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.ViewModels;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class AccountManagementViewModelShould
{
    [Fact]
    public async Task InitializeWithEmptyAccountCollectionWhenRepositoryIsEmpty()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken); // Allow async initialization

        viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task InitializeWithNullSelectedAccount()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public async Task LoadAccountsFromRepositoryOnInitialization()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var accounts = new[] { CreateAccount("acc1", "User 1"), CreateAccount("acc2", "User 2") };
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>(accounts));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts[0].AccountId.ShouldBe("acc1");
        viewModel.Accounts[1].AccountId.ShouldBe("acc2");
    }

    [Fact]
    public async Task RaisePropertyChangedWhenSelectedAccountChanges()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1");
        var propertyChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AccountManagementViewModel.SelectedAccount))
            {
                propertyChanged = true;
            }
        };

        viewModel.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
        viewModel.SelectedAccount.ShouldBe(account);
    }

    [Fact]
    public async Task NotRaisePropertyChangedWhenSettingSameSelectedAccount()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1");
        viewModel.SelectedAccount = account;

        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (_, _) => propertyChangedCount++;

        viewModel.SelectedAccount = account;

        propertyChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task RaisePropertyChangedWhenIsLoadingChanges()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var propertyChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AccountManagementViewModel.IsLoading))
            {
                propertyChanged = true;
            }
        };

        viewModel.IsLoading = true;

        propertyChanged.ShouldBeTrue();
        viewModel.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public async Task NotRaisePropertyChangedWhenSettingSameIsLoadingValue()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        viewModel.IsLoading = true;

        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (_, _) => propertyChangedCount++;

        viewModel.IsLoading = true;

        propertyChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task HaveAddAccountCommandAlwaysEnabled()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var canExecute = viewModel.AddAccountCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAddAccountCommandSuccessfully()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        var authResult = new AuthenticationResult(true, "acc1", "user@example.com", null);
        mockAuth.LoginAsync(Arg.Any<CancellationToken>()).Returns(authResult);

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        await viewModel.AddAccountCommand.Execute();

        await mockAuth.Received(1).LoginAsync(Arg.Any<CancellationToken>());
        await mockRepo.Received(1).AddAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HaveRemoveAccountCommandDisabledWhenNoAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var canExecute = viewModel.RemoveAccountCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task EnableRemoveAccountCommandWhenAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1");

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.RemoveAccountCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableRemoveAccountCommandWhenAccountDeselected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1");
        viewModel.SelectedAccount = account;

        viewModel.SelectedAccount = null;

        var canExecute = viewModel.RemoveAccountCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteRemoveAccountCommandWhenAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1");
        viewModel.SelectedAccount = account;

        await viewModel.RemoveAccountCommand.Execute();

        await mockRepo.Received(1).DeleteAsync("acc1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HaveLoginCommandDisabledWhenNoAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var canExecute = viewModel.LoginCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task EnableLoginCommandWhenUnauthenticatedAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1", isAuthenticated: false);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LoginCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableLoginCommandWhenAuthenticatedAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1", isAuthenticated: true);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LoginCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteLoginCommandSuccessfully()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        var authResult = new AuthenticationResult(true, "acc1", "User 1", null);
        mockAuth.LoginAsync(Arg.Any<CancellationToken>()).Returns(authResult);

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1", isAuthenticated: false);
        viewModel.SelectedAccount = account;

        await viewModel.LoginCommand.Execute();

        await mockAuth.Received(1).LoginAsync(Arg.Any<CancellationToken>());
        await mockRepo.Received(1).UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HaveLogoutCommandDisabledWhenNoAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var canExecute = viewModel.LogoutCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task EnableLogoutCommandWhenAuthenticatedAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1", isAuthenticated: true);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LogoutCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableLogoutCommandWhenUnauthenticatedAccountSelected()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1", isAuthenticated: false);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LogoutCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteLogoutCommandSuccessfully()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        mockAuth.LogoutAsync("acc1", Arg.Any<CancellationToken>()).Returns(true);

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1", isAuthenticated: true);
        viewModel.SelectedAccount = account;

        await viewModel.LogoutCommand.Execute();

        await mockAuth.Received(1).LogoutAsync("acc1", Arg.Any<CancellationToken>());
        await mockRepo.Received(1).UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AllowAddingAccountsToCollection()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account1 = CreateAccount("acc1", "User 1");
        var account2 = CreateAccount("acc2", "User 2");

        viewModel.Accounts.Add(account1);
        viewModel.Accounts.Add(account2);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts.ShouldContain(account1);
        viewModel.Accounts.ShouldContain(account2);
    }

    [Fact]
    public async Task AllowRemovingAccountsFromCollection()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var account = CreateAccount("acc1", "User 1");
        viewModel.Accounts.Add(account);

        viewModel.Accounts.Remove(account);

        viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisposeSuccessfullyWithoutErrors()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Should.NotThrow(viewModel.Dispose);
    }

    private static AccountInfo CreateAccount(string id, string displayName, bool isAuthenticated = false) =>
        new(id, displayName, $@"C:\Sync\{id}", isAuthenticated, null, null, false, false, 3, 50, null);
}
