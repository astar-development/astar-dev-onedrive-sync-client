using System.Collections.ObjectModel;
using System.Reactive;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using ReactiveUI;

namespace AStarOneDriveClient.ViewModels;

/// <summary>
/// ViewModel for the Debug Log Viewer window.
/// </summary>
public sealed class DebugLogViewModel : ReactiveObject
{
    private readonly IAccountRepository _accountRepository;
    private readonly IDebugLogRepository _debugLogRepository;
    private AccountInfo? _selectedAccount;
    private int _currentPage = 1;
    private bool _hasMoreRecords = true;
    private bool _isLoading;

    private const int PageSize = 50;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogViewModel"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="debugLogRepository">Repository for debug log entries.</param>
    public DebugLogViewModel(
        IAccountRepository accountRepository,
        IDebugLogRepository debugLogRepository)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(debugLogRepository);

        _accountRepository = accountRepository;
        _debugLogRepository = debugLogRepository;

        Accounts = new ObservableCollection<AccountInfo>();
        DebugLogs = new ObservableCollection<DebugLogEntry>();

        LoadNextPageCommand = ReactiveCommand.CreateFromTask(LoadNextPageAsync);
        LoadPreviousPageCommand = ReactiveCommand.CreateFromTask(LoadPreviousPageAsync);
        ClearLogsCommand = ReactiveCommand.CreateFromTask(ClearLogsAsync);

        // Load accounts on initialization
        _ = LoadAccountsAsync();
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    /// Gets the collection of debug log entries.
    /// </summary>
    public ObservableCollection<DebugLogEntry> DebugLogs { get; }

    /// <summary>
    /// Gets or sets the selected account.
    /// </summary>
    public AccountInfo? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAccount, value);
            if (value is not null)
            {
                CurrentPage = 1;
                _ = LoadDebugLogsAsync();
            }
        }
    }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>
    /// Gets a value indicating whether there are more records to load.
    /// </summary>
    public bool HasMoreRecords
    {
        get => _hasMoreRecords;
        private set => this.RaiseAndSetIfChanged(ref _hasMoreRecords, value);
    }

    /// <summary>
    /// Gets a value indicating whether data is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Gets a value indicating whether the user can navigate to the next page.
    /// </summary>
    public bool CanGoToNextPage => HasMoreRecords && !IsLoading;

    /// <summary>
    /// Gets a value indicating whether the user can navigate to the previous page.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1 && !IsLoading;

    /// <summary>
    /// Gets the command to load the next page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadNextPageCommand { get; }

    /// <summary>
    /// Gets the command to load the previous page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadPreviousPageCommand { get; }

    /// <summary>
    /// Gets the command to clear logs for the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }

    private async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
        }
        catch (Exception)
        {
            // Log or handle error if needed
        }
    }

    private async Task LoadDebugLogsAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var skip = (CurrentPage - 1) * PageSize;

            // Fetch PageSize + 1 to determine if there are more records
            var logs = await _debugLogRepository.GetByAccountIdAsync(
                SelectedAccount.AccountId,
                PageSize + 1,
                skip);

            DebugLogs.Clear();

            // If we got more than PageSize, there are more records
            HasMoreRecords = logs.Count > PageSize;

            // Only show PageSize records
            var logsToDisplay = logs.Take(PageSize);
            foreach (var log in logsToDisplay)
            {
                DebugLogs.Add(log);
            }

            // Notify property changes for navigation buttons
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
        }
        catch (Exception)
        {
            // Log or handle error if needed
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadNextPageAsync()
    {
        if (!HasMoreRecords || SelectedAccount is null)
        {
            return;
        }

        CurrentPage++;
        await LoadDebugLogsAsync();
    }

    private async Task LoadPreviousPageAsync()
    {
        if (CurrentPage <= 1 || SelectedAccount is null)
        {
            return;
        }

        CurrentPage--;
        await LoadDebugLogsAsync();
    }

    private async Task ClearLogsAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            await _debugLogRepository.DeleteByAccountIdAsync(SelectedAccount.AccountId);
            CurrentPage = 1;
            await LoadDebugLogsAsync();
        }
        catch (Exception)
        {
            // Log or handle error if needed
        }
        finally
        {
            IsLoading = false;
        }
    }
}
