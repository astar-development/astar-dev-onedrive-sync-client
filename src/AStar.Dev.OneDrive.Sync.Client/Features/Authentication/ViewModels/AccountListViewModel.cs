using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;

/// <summary>
/// ViewModel for the Account List UI.
/// Manages the collection of accounts and account selection logic.
/// </summary>
public class AccountListViewModel : ReactiveObject
{
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountListViewModel"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository for loading account data.</param>
    public AccountListViewModel(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        LoadAccountsCommand = ReactiveCommand.CreateFromTask(LoadAccountsAsync);
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<Account> Accounts { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected account.
    /// </summary>
    public Account? SelectedAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether accounts are being loaded.
    /// </summary>
    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the command to load accounts from the repository.
    /// </summary>
    public ICommand LoadAccountsCommand { get; }

    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        
        try
        {
            Accounts.Clear();
            var accounts = await _accountRepository.GetAllAsync();
            
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
