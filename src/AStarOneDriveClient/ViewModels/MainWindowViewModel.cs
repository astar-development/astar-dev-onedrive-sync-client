using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Views;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace AStarOneDriveClient.ViewModels;

/// <summary>
/// ViewModel for the main application window, coordinating between account management and sync tree views.
/// </summary>
public sealed class MainWindowViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IServiceProvider _serviceProvider;
    private readonly IAutoSyncCoordinator _autoSyncCoordinator;
    private readonly IAccountRepository _accountRepository;
    private readonly ISyncConflictRepository _conflictRepository;

    /// <summary>
    /// Gets the sync progress view model (when sync is active).
    /// </summary>
    public SyncProgressViewModel? SyncProgress
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the conflict resolution view model (when viewing conflicts).
    /// </summary>
    public ConflictResolutionViewModel? ConflictResolution
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether sync progress view should be shown.
    /// </summary>
    public bool ShowSyncProgress => SyncProgress is not null && ConflictResolution is null;

    /// <summary>
    /// Gets a value indicating whether conflict resolution view should be shown.
    /// </summary>
    public bool ShowConflictResolution => ConflictResolution is not null;

    /// <summary>
    /// Gets a value indicating whether the selected account has unresolved conflicts.
    /// </summary>
    public bool HasUnresolvedConflicts
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="accountManagementViewModel">The account management view model.</param>
    /// <param name="syncTreeViewModel">The sync tree view model.</param>
    /// <param name="serviceProvider">The service provider for creating SyncProgressViewModel instances.</param>
    /// <param name="autoSyncCoordinator">The auto-sync coordinator for file watching.</param>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="conflictRepository">Repository for sync conflicts.</param>
    public MainWindowViewModel(
        AccountManagementViewModel accountManagementViewModel,
        SyncTreeViewModel syncTreeViewModel,
        IServiceProvider serviceProvider,
        IAutoSyncCoordinator autoSyncCoordinator,
        IAccountRepository accountRepository,
        ISyncConflictRepository conflictRepository)
    {
        ArgumentNullException.ThrowIfNull(accountManagementViewModel);
        ArgumentNullException.ThrowIfNull(syncTreeViewModel);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(autoSyncCoordinator);
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(conflictRepository);

        AccountManagement = accountManagementViewModel;
        SyncTree = syncTreeViewModel;
        _serviceProvider = serviceProvider;
        _autoSyncCoordinator = autoSyncCoordinator;
        _accountRepository = accountRepository;
        _conflictRepository = conflictRepository;

        // Wire up: When an account is selected, update the sync tree
        accountManagementViewModel
            .WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account?.AccountId)
            .BindTo(syncTreeViewModel, x => x.SelectedAccountId)
            .DisposeWith(_disposables);

        // Wire up: Check for unresolved conflicts when account selection changes
        accountManagementViewModel
            .WhenAnyValue(x => x.SelectedAccount)
            .Subscribe(async account =>
            {
                if (account is not null)
                {
                    await UpdateConflictStatusAsync(account.AccountId);
                }
                else
                {
                    HasUnresolvedConflicts = false;
                }
            })
            .DisposeWith(_disposables);

        // Wire up: When sync completes successfully, start auto-sync monitoring
        syncTreeViewModel
            .WhenAnyValue(x => x.SyncState)
            .Where(state => state.Status == Models.Enums.SyncStatus.Completed && !string.IsNullOrEmpty(state.AccountId))
            .Subscribe(async state =>
            {
                try
                {
                    // Get account info to retrieve local sync path
                    var account = await _accountRepository.GetByIdAsync(state.AccountId);
                    if (account is not null && !string.IsNullOrEmpty(account.LocalSyncPath))
                    {
                        await _autoSyncCoordinator.StartMonitoringAsync(state.AccountId, account.LocalSyncPath);
                    }
                }
                catch
                {
                    // Silently fail - auto-sync is a convenience feature
                    // Manual sync will still work
                }
            })
            .DisposeWith(_disposables);

        // Wire up: When sync starts, show sync progress view
        syncTreeViewModel
            .WhenAnyValue(x => x.IsSyncing, x => x.SelectedAccountId,
                (isSyncing, accountId) => new { IsSyncing = isSyncing, AccountId = accountId })
            .Subscribe(state =>
            {
                if (state.IsSyncing && !string.IsNullOrEmpty(state.AccountId))
                {
                    // Create sync progress view model if not already created
                    if (SyncProgress is null || SyncProgress.AccountId != state.AccountId)
                    {
                        SyncProgress?.Dispose();
                        var syncProgressVm = ActivatorUtilities.CreateInstance<SyncProgressViewModel>(
                            _serviceProvider,
                            state.AccountId);

                        // Wire up ViewConflictsCommand to show conflict resolution
                        syncProgressVm.ViewConflictsCommand
                            .Subscribe(_ => ShowConflictResolutionView(state.AccountId))
                            .DisposeWith(_disposables);

                        // Wire up CloseCommand to dismiss sync progress view
                        syncProgressVm.CloseCommand
                            .Subscribe(_ => CloseSyncProgressView())
                            .DisposeWith(_disposables);

                        // Auto-close overlay when sync completes successfully without conflicts
                        // Note: Do NOT auto-close on Failed status - user needs to see error details
                        syncProgressVm.WhenAnyValue(x => x.CurrentProgress)
                            .Where(progress => progress is not null &&
                                   progress.Status == SyncStatus.Completed &&
                                   progress.ConflictsDetected == 0)
                            .Delay(TimeSpan.FromSeconds(2)) // Show completion message briefly
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(_ => CloseSyncProgressView())
                            .DisposeWith(_disposables);

                        // Update conflict status when sync completes
                        syncProgressVm.WhenAnyValue(x => x.CurrentProgress)
                            .Where(progress => progress is not null && progress.Status == SyncStatus.Completed)
                            .Subscribe(async _ => await UpdateConflictStatusAsync(state.AccountId))
                            .DisposeWith(_disposables);

                        SyncProgress = syncProgressVm;
                    }
                }
                else if (!state.IsSyncing && SyncProgress is not null)
                {
                    // Keep showing progress view even when not syncing (shows completion/pause state)
                    // User can manually close by navigating away
                }

                this.RaisePropertyChanged(nameof(ShowSyncProgress));
                this.RaisePropertyChanged(nameof(ShowConflictResolution));
            })
            .DisposeWith(_disposables);

        // Commands
        OpenUpdateAccountDetailsCommand = ReactiveCommand.Create(OpenUpdateAccountDetails);
        OpenViewSyncHistoryCommand = ReactiveCommand.Create(OpenViewSyncHistory);
        OpenDebugLogViewerCommand = ReactiveCommand.Create(OpenDebugLogViewer);
        ViewConflictsCommand = ReactiveCommand.Create(ViewConflicts, this.WhenAnyValue(x => x.HasUnresolvedConflicts));
        CloseApplicationCommand = ReactiveCommand.Create(CloseApplication);
    }

    /// <summary>
    /// Gets the account management view model.
    /// </summary>
    public AccountManagementViewModel AccountManagement { get; }

    /// <summary>
    /// Gets the sync tree view model.
    /// </summary>
    public SyncTreeViewModel SyncTree { get; }

    /// <summary>
    /// Gets the command to open the Update Account Details window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenUpdateAccountDetailsCommand { get; }

    /// <summary>
    /// Gets the command to close the application.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseApplicationCommand { get; }

    /// <summary>
    /// Gets the command to open the View Sync History window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenViewSyncHistoryCommand { get; }

    /// <summary>
    /// Gets the command to open the Debug Log Viewer window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenDebugLogViewerCommand { get; }

    /// <summary>
    /// Gets the command to view unresolved conflicts.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewConflictsCommand { get; }

    /// <summary>
    /// Shows the conflict resolution view for the specified account.
    /// </summary>
    /// <param name="accountId">The account ID to show conflicts for.</param>
    private void ShowConflictResolutionView(string accountId)
    {
        ConflictResolution?.Dispose();
        var conflictResolutionVm = ActivatorUtilities.CreateInstance<ConflictResolutionViewModel>(
            _serviceProvider,
            accountId);

        // Wire up CancelCommand to return to sync progress
        conflictResolutionVm.CancelCommand
            .Subscribe(_ => CloseConflictResolutionView())
            .DisposeWith(_disposables);

        // Wire up ResolveAllCommand to return to sync progress after resolution
        conflictResolutionVm.ResolveAllCommand
            .Subscribe(_ =>
            {
                // Delay closing to allow user to see the status message
                Observable.Timer(TimeSpan.FromSeconds(2))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => CloseConflictResolutionView())
                    .DisposeWith(_disposables);
            })
            .DisposeWith(_disposables);

        ConflictResolution = conflictResolutionVm;
        this.RaisePropertyChanged(nameof(ShowSyncProgress));
        this.RaisePropertyChanged(nameof(ShowConflictResolution));
    }

    /// <summary>
    /// Closes the conflict resolution view and returns to sync progress.
    /// </summary>
    private async void CloseConflictResolutionView()
    {
        ConflictResolution?.Dispose();
        ConflictResolution = null;
        this.RaisePropertyChanged(nameof(ShowSyncProgress));
        this.RaisePropertyChanged(nameof(ShowConflictResolution));

        // Refresh conflict count after resolving conflicts
        if (SyncProgress is not null)
        {
            await SyncProgress.RefreshConflictCountAsync();
        }

        // Update main window conflict status
        if (AccountManagement.SelectedAccount is not null)
        {
            await UpdateConflictStatusAsync(AccountManagement.SelectedAccount.AccountId);
        }
    }

    /// <summary>
    /// Updates the conflict status indicator for the specified account.
    /// </summary>
    /// <param name="accountId">The account ID to check.</param>
    private async Task UpdateConflictStatusAsync(string accountId)
    {
        var conflicts = await _conflictRepository.GetUnresolvedByAccountIdAsync(accountId);
        HasUnresolvedConflicts = conflicts.Any();
    }

    /// <summary>
    /// Opens the conflict resolution view for the selected account.
    /// </summary>
    private void ViewConflicts()
    {
        if (AccountManagement.SelectedAccount is not null)
        {
            ShowConflictResolutionView(AccountManagement.SelectedAccount.AccountId);
        }
    }

    /// <summary>
    /// Closes the sync progress view and returns to sync tree.
    /// </summary>
    private void CloseSyncProgressView()
    {
        SyncProgress?.Dispose();
        SyncProgress = null;
        ConflictResolution?.Dispose();
        ConflictResolution = null;
        this.RaisePropertyChanged(nameof(ShowSyncProgress));
        this.RaisePropertyChanged(nameof(ShowConflictResolution));
    }

    /// <summary>
    /// Opens the Update Account Details window.
    /// </summary>
    private void OpenUpdateAccountDetails()
    {
        var window = new UpdateAccountDetailsWindow();

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            _ = window.ShowDialog(desktop.MainWindow);
        }
    }

    /// <summary>
    /// Opens the View Sync History window.
    /// </summary>
    private void OpenViewSyncHistory()
    {
        var window = new ViewSyncHistoryWindow();

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            _ = window.ShowDialog(desktop.MainWindow);
        }
    }

    /// <summary>
    /// Opens the Debug Log Viewer window.
    /// </summary>
    private void OpenDebugLogViewer()
    {
        var window = new DebugLogWindow();

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            _ = window.ShowDialog(desktop.MainWindow);
        }
    }

    /// <summary>
    /// Closes the application.
    /// </summary>
    private void CloseApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposables.Dispose();
        _autoSyncCoordinator.Dispose();
        ConflictResolution?.Dispose();
        SyncProgress?.Dispose();
        AccountManagement.Dispose();
        SyncTree.Dispose();
    }
}
