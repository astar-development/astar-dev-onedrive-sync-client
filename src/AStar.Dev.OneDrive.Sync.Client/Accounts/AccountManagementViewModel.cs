using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

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
        RemoveAccountCommand = ReactiveCommand.CreateFromTask(RemoveAccountAsync, canRemove, RxApp.MainThreadScheduler);

        IObservable<bool> canLogin = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && !account.IsAuthenticated);
        LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync, canLogin, RxApp.MainThreadScheduler);

        IObservable<bool> canLogout = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && account.IsAuthenticated);
        LogoutCommand = ReactiveCommand.CreateFromTask(LogoutAsync, canLogout, RxApp.MainThreadScheduler);

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
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddAccountCommand { get; }

    /// <summary>
    ///     Gets the command to remove the selected account.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RemoveAccountCommand { get; }

    /// <summary>
    ///     Gets the command to login to the selected account.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LoginCommand { get; }

    /// <summary>
    ///     Gets the command to logout from the selected account.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LogoutCommand { get; }

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
            CancellationToken cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

            Result<AuthenticationResult, ErrorResponse> result = await _authService.LoginAsync(cancellationToken);
            _ = result.Match<AStar.Dev.Functional.Extensions.Unit>(
                authResult =>
                {
                    if(authResult.DisplayName is not null)
                    {
                        var localSyncPath = CreateTheLocalSyncPath(authResult);
                        var newAccount = AccountInfo.Standard(authResult.AccountId, authResult.HashedAccountId, authResult.DisplayName, localSyncPath);
                        _ = _accountRepository.AddAsync(newAccount);
                        Accounts.Add(newAccount);
                        SelectedAccount = newAccount;
                    }

                    return AStar.Dev.Functional.Extensions.Unit.Value;
                },
                error =>
                {
                    _ = ShowToastAsync(error.Message);

                    return AStar.Dev.Functional.Extensions.Unit.Value;
                });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string CreateTheLocalSyncPath(AuthenticationResult result)
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ApplicationMetadata.ApplicationFolder, result.DisplayName.Replace("@", "_").Replace(".", "_"));

    private async Task RemoveAccountAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            await _accountRepository.DeleteAsync(SelectedAccount.Id);
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

            CancellationToken cancellationToken = CancellationToken.None;
            Result<AuthenticationResult, ErrorResponse> result = await _authService.LoginAsync(cancellationToken);
            _ = result.Match<AStar.Dev.Functional.Extensions.Unit>(
                authResult =>
                {
                    if(authResult.DisplayName is not null)
                    {
                        AccountInfo updatedAccount = SelectedAccount with { IsAuthenticated = true };
                        _ = _accountRepository.UpdateAsync(updatedAccount);

                        var index = Accounts.IndexOf(SelectedAccount);
                        if(index >= 0)
                        {
                            Accounts[index] = updatedAccount;
                            SelectedAccount = updatedAccount;
                            using(Serilog.Context.LogContext.PushProperty("AccountHash", updatedAccount.HashedAccountId))
                            {
                                _logger.LogInformation("Starting sync for account");
                            }
                        }
                    }

                    return AStar.Dev.Functional.Extensions.Unit.Value;
                },
                error =>
                {
                    _ = ShowToastAsync(error.Message);

                    return AStar.Dev.Functional.Extensions.Unit.Value;
                });
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
            Result<bool, ErrorResponse> logoutResult = await _authService.LogoutAsync(SelectedAccount.Id);
            _ = logoutResult.Match<AStar.Dev.Functional.Extensions.Unit>(
                _ =>
                {
                    AccountInfo updatedAccount = SelectedAccount with { IsAuthenticated = false };
                    Task updateTask = _accountRepository.UpdateAsync(updatedAccount);

                    var index = Accounts.IndexOf(SelectedAccount);
                    if(index >= 0)
                    {
                        Accounts[index] = updatedAccount;
                        SelectedAccount = updatedAccount;
                    }

                    return AStar.Dev.Functional.Extensions.Unit.Value;
                },
                error =>
                {
                    _logger.LogError("Logout failed for account {AccountId}: {ErrorMessage}", SelectedAccount.Id, error.Message);
                    _ = ShowToastAsync($"Logout failed: {error.Message}");

                    return AStar.Dev.Functional.Extensions.Unit.Value;
                });
        }
        finally
        {
            IsLoading = false;
        }
    }
}
