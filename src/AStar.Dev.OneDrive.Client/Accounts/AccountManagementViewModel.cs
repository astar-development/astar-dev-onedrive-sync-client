using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.Accounts;

/// <summary>
///     ViewModel for managing OneDrive accounts.
/// </summary>
public sealed class AccountManagementViewModel : ReactiveObject, IDisposable
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountManagementViewModel> _logger;
    private readonly IAuthService _authService;
    private readonly CompositeDisposable _disposables = [];

    private CancellationTokenSource? _toastCts;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AccountManagementViewModel" /> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="accountRepository">The account repository.</param>
    public AccountManagementViewModel(IAuthService authService, IAccountRepository accountRepository, ILogger<AccountManagementViewModel> logger)
    {
        _authService = authService;
        _accountRepository = accountRepository;
        _logger = logger;
        Accounts = [];

        AddAccountCommand = ReactiveCommand.CreateFromTask(AddAccountAsync, outputScheduler: RxApp.MainThreadScheduler);

        IObservable<bool> canRemove = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null);
        RemoveAccountCommand = ReactiveCommand.CreateFromTask(
            RemoveAccountAsync,
            canRemove,
            RxApp.MainThreadScheduler);

        IObservable<bool> canLogin = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && !account.IsAuthenticated);
        LoginCommand = ReactiveCommand.CreateFromTask(
            LoginAsync,
            canLogin,
            RxApp.MainThreadScheduler);

        IObservable<bool> canLogout = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && account.IsAuthenticated);
        LogoutCommand = ReactiveCommand.CreateFromTask(
            LogoutAsync,
            canLogout,
            RxApp.MainThreadScheduler);

        _ = canRemove.Subscribe().DisposeWith(_disposables);
        _ = canLogin.Subscribe().DisposeWith(_disposables);
        _ = canLogout.Subscribe().DisposeWith(_disposables);

        _ = LoadAccountsAsync();
    }

    /// <summary>
    ///     Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    ///     Gets or sets the transient toast message to show to the user.
    /// </summary>
    public string? ToastMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets or sets whether the toast is currently visible.
    /// </summary>
    public bool ToastVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the currently selected account.
    /// </summary>
    public AccountInfo? SelectedAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the view is loading data.
    /// </summary>
    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets the command to add a new account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }

    /// <summary>
    ///     Gets the command to remove the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveAccountCommand { get; }

    /// <summary>
    ///     Gets the command to login to the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoginCommand { get; }

    /// <summary>
    ///     Gets the command to logout from the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    /// <inheritdoc />
    public void Dispose() => _disposables.Dispose();

    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        try
        {
            IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach(AccountInfo account in accounts)
                Accounts.Add(account);
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
            _toastCts?.Cancel();
            ToastMessage = null;
            ToastVisible = false;

            AuthenticationResult result = await _authService.LoginAsync();
            if(result is { Success: true, AccountId: not null, DisplayName: not null })
            {
                var defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ApplicationMetadata.ApplicationFolder,
                    result.DisplayName.Replace("@", "_").Replace(".", "_"));

                var newAccount = new AccountInfo(
                    result.AccountId,
                    result.DisplayName,
                    defaultPath,
                    true,
                    null,
                    null,
                    false,
                    false,
                    5,
                    50,
                    0);

                await _accountRepository.AddAsync(newAccount);
                Accounts.Add(newAccount);
                SelectedAccount = newAccount;
            }
            else if(result is { Success: false, ErrorMessage: not null })
            {
                _ = ShowToastAsync(result.ErrorMessage);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RemoveAccountAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            await _accountRepository.DeleteAsync(SelectedAccount.AccountId);
            _ = Accounts.Remove(SelectedAccount);
            SelectedAccount = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoginAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            _toastCts?.Cancel();
            ToastMessage = null;
            ToastVisible = false;

            AuthenticationResult result = await _authService.LoginAsync();
            if(result.Success)
            {
                AccountInfo updatedAccount = SelectedAccount with { IsAuthenticated = true };
                await _accountRepository.UpdateAsync(updatedAccount);

                var index = Accounts.IndexOf(SelectedAccount);
                if(index >= 0)
                {
                    Accounts[index] = updatedAccount;
                    SelectedAccount = updatedAccount;
                    var accountHash = AccountIdHasher.Hash(updatedAccount.AccountId); 
                    using (Serilog.Context.LogContext.PushProperty("AccountHash", accountHash))
                    {
                        _logger.LogInformation("Starting sync for account");
                    }
                }
            }
            else if(result is { Success: false, ErrorMessage: not null })
            {
                _ = ShowToastAsync(result.ErrorMessage);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowToastAsync(string message)
    {
        try
        {
            _toastCts?.Cancel();
            _toastCts = new CancellationTokenSource();
            ToastMessage = message;
            ToastVisible = true;

            await Task.Delay(TimeSpan.FromSeconds(5), _toastCts.Token);

            // Clear toast if not cancelled
            ToastVisible = false;
            ToastMessage = null;
        }
        catch(TaskCanceledException)
        {
            // Ignore - a new toast was requested
        }
        finally
        {
            _toastCts?.Dispose();
            _toastCts = null;
        }
    }

    private async Task LogoutAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            var success = await _authService.LogoutAsync(SelectedAccount.AccountId);
            if(success)
            {
                AccountInfo updatedAccount = SelectedAccount with { IsAuthenticated = false };
                await _accountRepository.UpdateAsync(updatedAccount);

                var index = Accounts.IndexOf(SelectedAccount);
                if(index >= 0)
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
}
