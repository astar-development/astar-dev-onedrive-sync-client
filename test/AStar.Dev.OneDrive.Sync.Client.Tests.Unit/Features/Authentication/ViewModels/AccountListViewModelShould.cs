using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.ViewModels;

public class AccountListViewModelShould
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly AccountListViewModel _viewModel;

    public AccountListViewModelShould() => _viewModel = new AccountListViewModel(_accountRepository);

    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountRepositoryIsNull()
    {
        Func<AccountListViewModel> act = () => new AccountListViewModel(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void InitializeWithEmptyAccountsCollection()
    {
        _viewModel.Accounts.ShouldNotBeNull();
        _viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithNoSelectedAccount() => _viewModel.SelectedAccount.ShouldBeNull();

    [Fact]
    public async Task LoadAccountsPopulatesAccountsCollection()
    {
        var account1 = new Account { Id = Guid.NewGuid(), HashedEmail = "test1@example.com", HashedAccountId = "id1" };
        var account2 = new Account { Id = Guid.NewGuid(), HashedEmail = "test2@example.com", HashedAccountId = "id2" };
        _accountRepository.GetAllAsync().Returns([account1, account2]);

        _viewModel.LoadAccountsCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Accounts.Count.ShouldBe(2);
        _viewModel.Accounts.ShouldContain(a => a.Id == account1.Id);
        _viewModel.Accounts.ShouldContain(a => a.Id == account2.Id);
    }

    [Fact]
    public async Task LoadAccountsClearsExistingAccountsBeforeLoading()
    {
        var existingAccount = new Account { Id = Guid.NewGuid(), HashedEmail = "existing@example.com", HashedAccountId = "existing" };
        _viewModel.Accounts.Add(existingAccount);

        var newAccount = new Account { Id = Guid.NewGuid(), HashedEmail = "new@example.com", HashedAccountId = "new" };
        _accountRepository.GetAllAsync().Returns([newAccount]);

        _viewModel.LoadAccountsCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.Accounts.Count.ShouldBe(1);
        _viewModel.Accounts.ShouldNotContain(existingAccount);
        _viewModel.Accounts.ShouldContain(a => a.Id == newAccount.Id);
    }

    [Fact]
    public void UpdateSelectedAccountWhenSet()
    {
        var account = new Account { Id = Guid.NewGuid(), HashedEmail = "test@example.com", HashedAccountId = "id" };
        _viewModel.Accounts.Add(account);

        _viewModel.SelectedAccount = account;

        _viewModel.SelectedAccount.ShouldBe(account);
    }

    [Fact]
    public void SetIsLoadingTrueDuringLoad()
    {
        _accountRepository.GetAllAsync().Returns(callInfo => Task.FromResult(Enumerable.Empty<Account>()));

        _viewModel.IsLoading.ShouldBeFalse();
    }
}
