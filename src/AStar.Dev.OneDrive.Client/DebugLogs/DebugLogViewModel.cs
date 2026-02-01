using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.Logging.Extensions.Serilog;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.DebugLogs;

/// <summary>
///     ViewModel for the Debug Log Viewer window.
/// </summary>
public sealed class DebugLogViewModel : ReactiveObject
{
    private const int _pageSize = 50;
    private readonly IAccountRepository _accountRepository;
    private readonly IDebugLogRepository _debugLogRepository;
    private readonly IDebugLogger _debugLogger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DebugLogViewModel" /> class.
    /// </summary>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="debugLogRepository">Repository for debug log entries.</param>
    /// <param name="debugLogger">Service for logging debug information.</param>
    public DebugLogViewModel(IAccountRepository accountRepository, IDebugLogRepository debugLogRepository, IDebugLogger debugLogger)
    {
        _accountRepository = accountRepository;
        _debugLogRepository = debugLogRepository;
        _debugLogger = debugLogger;

        Accounts = [];
        DebugLogs = [];

        LoadNextPageCommand = ReactiveCommand.CreateFromTask(LoadNextPageAsync);
        LoadPreviousPageCommand = ReactiveCommand.CreateFromTask(LoadPreviousPageAsync);
        ClearLogsCommand = ReactiveCommand.CreateFromTask(ClearLogsAsync);

        _ = LoadAccountsAsync();
    }

    /// <summary>
    ///     Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    ///     Gets the collection of debug log entries.
    /// </summary>
    public ObservableCollection<DebugLogEntry> DebugLogs { get; }

    /// <summary>
    ///     Gets or sets the selected account.
    /// </summary>
    public AccountInfo? SelectedAccount
    {
        get;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            if(value is not null)
            {
                CurrentPage = 1;
                _ = LoadAllLogsAsync(ApplicationMetadata.ApplicationFolder);
            }
        }
    }

    /// <summary>
    ///     Gets the current page number.
    /// </summary>
    public int CurrentPage
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
        }
    } = 1;

    /// <summary>
    ///     Gets a value indicating whether there are more records to load.
    /// </summary>
    public bool HasMoreRecords
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
        }
    } = true;

    /// <summary>
    ///     Gets a value indicating whether data is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
        }
    }

    /// <summary>
    ///     Gets the total number of records available.
    /// </summary>
    public int TotalRecordCount
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets a value indicating whether the user can navigate to the next page.
    /// </summary>
    public bool CanGoToNextPage => HasMoreRecords && !IsLoading;

    /// <summary>
    ///     Gets a value indicating whether the user can navigate to the previous page.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1 && !IsLoading;

    /// <summary>
    ///     Gets the command to load the next page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadNextPageCommand { get; }

    /// <summary>
    ///     Gets the command to load the previous page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadPreviousPageCommand { get; }

    /// <summary>
    ///     Gets the command to clear logs for the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }

    private async Task LoadAccountsAsync()
    {
        try
        {
            IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach(AccountInfo account in accounts)
                Accounts.Add(account);
        }
        catch(Exception)
        {
            // Log or handle error if needed
        }
    }

    public async Task LoadAllLogsAsync(string applicationFolder)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            applicationFolder, "logs");
        IEnumerable<string> allFiles = SerilogLogFileLocator.GetAllLogFiles(logDir);

        DebugLogs.Clear();

        var id = 1;

        var accountHash = SelectedAccount is not null
            ? AccountIdHasher.Hash(SelectedAccount.AccountId)
            : string.Empty;
            
        foreach (var file in allFiles.Where(f => f.Contains(accountHash)))
        {
            var lines = await File.ReadAllLinesAsync(file);

            foreach (var line in lines)
            {
                DebugLogEntry? entry = SerilogLogParser.Parse(line, id++);
                if (entry is not null)
                    DebugLogs.Add(entry);
            }
        }
    }

    private async Task LoadNextPageAsync()
    {
        if(!HasMoreRecords || SelectedAccount is null)
            return;

        CurrentPage++;
        await LoadAllLogsAsync(ApplicationMetadata.ApplicationFolder);
    }

    private async Task LoadPreviousPageAsync()
    {
        if(CurrentPage <= 1 || SelectedAccount is null)
            return;

        CurrentPage--;
        await LoadAllLogsAsync(ApplicationMetadata.ApplicationFolder);
    }

    private async Task ClearLogsAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            await _debugLogRepository.DeleteByAccountIdAsync(SelectedAccount.AccountId);
            CurrentPage = 1;
            await LoadAllLogsAsync(ApplicationMetadata.ApplicationFolder);
        }
        catch(Exception)
        {
            // Log or handle error if needed
        }
        finally
        {
            IsLoading = false;
        }
    }
}
