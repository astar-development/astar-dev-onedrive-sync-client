using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using ReactiveUI;

namespace AStarOneDriveClient.ViewModels;

/// <summary>
/// ViewModel for managing OneDrive accounts.
/// </summary>
public sealed class AccountManagementViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IAuthService _authService;
    private readonly IAccountRepository _accountRepository;
    private AccountInfo? _selectedAccount;
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountManagementViewModel"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="accountRepository">The account repository.</param>
    public AccountManagementViewModel(IAuthService authService, IAccountRepository accountRepository)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(accountRepository);

        _authService = authService;
        _accountRepository = accountRepository;
        Accounts = new ObservableCollection<AccountInfo>();

        // Add Account command - always enabled
        AddAccountCommand = ReactiveCommand.CreateFromTask(
            AddAccountAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        // Remove Account command - enabled when account is selected
        var canRemove = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null);
        RemoveAccountCommand = ReactiveCommand.CreateFromTask(
            RemoveAccountAsync,
            canRemove,
            RxApp.MainThreadScheduler);

        // Login command - enabled when account is selected and not authenticated
        var canLogin = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && !account.IsAuthenticated);
        LoginCommand = ReactiveCommand.CreateFromTask(
            LoginAsync,
            canLogin,
            RxApp.MainThreadScheduler);

        // Logout command - enabled when account is selected and authenticated
        var canLogout = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && account.IsAuthenticated);
        LogoutCommand = ReactiveCommand.CreateFromTask(
            LogoutAsync,
            canLogout,
            RxApp.MainThreadScheduler);

        // Dispose observables
        canRemove.Subscribe().DisposeWith(_disposables);
        canLogin.Subscribe().DisposeWith(_disposables);
        canLogout.Subscribe().DisposeWith(_disposables);

        // Load accounts on initialization
        _ = LoadAccountsAsync();
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    /// Gets or sets the currently selected account.
    /// </summary>
    public AccountInfo? SelectedAccount
    {
        get => _selectedAccount;
        set => this.RaiseAndSetIfChanged(ref _selectedAccount, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the view is loading data.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Gets the command to add a new account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }

    /// <summary>
    /// Gets the command to remove the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveAccountCommand { get; }

    /// <summary>
    /// Gets the command to login to the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoginCommand { get; }

    /// <summary>
    /// Gets the command to logout from the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        try
        {
            var accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
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

    private async Task AddAccountAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _authService.LoginAsync();
            if (result.Success && result.AccountId is not null && result.DisplayName is not null)
            {
                // Create new account with default sync path
                var defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AStarOneDrive",
                    result.DisplayName.Replace("@", "_").Replace(".", "_"));

                var newAccount = new AccountInfo(
                    AccountId: result.AccountId,
                    DisplayName: result.DisplayName,
                    LocalSyncPath: defaultPath,
                    IsAuthenticated: true,
                    LastSyncUtc: null,
                    DeltaToken: null,
                    EnableDetailedSyncLogging: false);

                await _accountRepository.AddAsync(newAccount);
                Accounts.Add(newAccount);
                SelectedAccount = newAccount;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RemoveAccountAsync()
    {
        if (SelectedAccount is null) return;

        IsLoading = true;
        try
        {
            await _accountRepository.DeleteAsync(SelectedAccount.AccountId);
            Accounts.Remove(SelectedAccount);
            SelectedAccount = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoginAsync()
    {
        if (SelectedAccount is null) return;

        IsLoading = true;
        try
        {
            var result = await _authService.LoginAsync();
            if (result.Success)
            {
                var updatedAccount = SelectedAccount with { IsAuthenticated = true };
                await _accountRepository.UpdateAsync(updatedAccount);

                var index = Accounts.IndexOf(SelectedAccount);
                if (index >= 0)
                {
                    Accounts[index] = updatedAccount;
                    SelectedAccount = updatedAccount;
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LogoutAsync()
    {
        if (SelectedAccount is null) return;

        IsLoading = true;
        try
        {
            var success = await _authService.LogoutAsync(SelectedAccount.AccountId);
            if (success)
            {
                var updatedAccount = SelectedAccount with { IsAuthenticated = false };
                await _accountRepository.UpdateAsync(updatedAccount);

                var index = Accounts.IndexOf(SelectedAccount);
                if (index >= 0)
                {
                    Accounts[index] = updatedAccount;
                    SelectedAccount = updatedAccount;
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
