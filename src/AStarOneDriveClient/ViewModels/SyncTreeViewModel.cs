using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Services.OneDriveServices;
using ReactiveUI;

namespace AStarOneDriveClient.ViewModels;

/// <summary>
/// ViewModel for the sync tree view, managing folder hierarchy and selection.
/// </summary>
public sealed class SyncTreeViewModel : ReactiveObject, IDisposable
{
    private readonly IFolderTreeService _folderTreeService;
    private readonly ISyncSelectionService _selectionService;
    private readonly ISyncEngine _syncEngine;
    private readonly CompositeDisposable _disposables = new();

    private string? _selectedAccountId;
    /// <summary>
    /// Gets or sets the currently selected account ID.
    /// </summary>
    public string? SelectedAccountId
    {
        get => _selectedAccountId;
        set => this.RaiseAndSetIfChanged(ref _selectedAccountId, value);
    }

    private ObservableCollection<OneDriveFolderNode> _rootFolders = [];
    /// <summary>
    /// Gets the root-level folders for the selected account.
    /// </summary>
    public ObservableCollection<OneDriveFolderNode> RootFolders
    {
        get => _rootFolders;
        private set => this.RaiseAndSetIfChanged(ref _rootFolders, value);
    }

    private bool _isLoading;
    /// <summary>
    /// Gets a value indicating whether folders are currently being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string? _errorMessage;
    /// <summary>
    /// Gets the error message if loading fails.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private SyncState _syncState = new(
        AccountId: string.Empty,
        Status: SyncStatus.Idle,
        TotalFiles: 0,
        CompletedFiles: 0,
        TotalBytes: 0,
        CompletedBytes: 0,
        FilesDownloading: 0,
        FilesUploading: 0,
        ConflictsDetected: 0,
        MegabytesPerSecond: 0,
        EstimatedSecondsRemaining: null,
        LastUpdateUtc: null);

    /// <summary>
    /// Gets the current sync state.
    /// </summary>
    public SyncState SyncState
    {
        get => _syncState;
        private set => this.RaiseAndSetIfChanged(ref _syncState, value);
    }

    /// <summary>
    /// Gets a value indicating whether sync is currently running.
    /// </summary>
    public bool IsSyncing => SyncState.Status == SyncStatus.Running;

    /// <summary>
    /// Gets sync progress as percentage (0-100).
    /// </summary>
    public double ProgressPercentage => SyncState.TotalFiles > 0
        ? (double)SyncState.CompletedFiles / SyncState.TotalFiles * 100
        : 0;

    /// <summary>
    /// Gets sync progress text for display.
    /// </summary>
    public string ProgressText => SyncState.Status switch
    {
        SyncStatus.Idle => "Ready to sync",
        SyncStatus.Running => $"Syncing {SyncState.CompletedFiles} of {SyncState.TotalFiles} files",
        SyncStatus.Completed => $"Sync completed - {SyncState.TotalFiles} files",
        SyncStatus.Paused => "Sync paused",
        SyncStatus.Failed => "Sync failed",
        _ => string.Empty
    };

    /// <summary>
    /// Command to load root folders for the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFoldersCommand { get; }

    /// <summary>
    /// Command to load child folders when a node is expanded.
    /// </summary>
    public ReactiveCommand<OneDriveFolderNode, Unit> LoadChildrenCommand { get; }

    /// <summary>
    /// Command to toggle folder selection state.
    /// </summary>
    public ReactiveCommand<OneDriveFolderNode, Unit> ToggleSelectionCommand { get; }

    /// <summary>
    /// Command to clear all selections.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearSelectionsCommand { get; }

    /// <summary>
    /// Command to start synchronization.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSyncCommand { get; }

    /// <summary>
    /// Command to stop synchronization.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopSyncCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncTreeViewModel"/> class.
    /// </summary>
    /// <param name="folderTreeService">Service for loading folder hierarchies.</param>
    /// <param name="selectionService">Service for managing selection state.</param>
    /// <param name="syncEngine">Service for file synchronization.</param>
    public SyncTreeViewModel(
        IFolderTreeService folderTreeService,
        ISyncSelectionService selectionService,
        ISyncEngine syncEngine)
    {
        _folderTreeService = folderTreeService ?? throw new ArgumentNullException(nameof(folderTreeService));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _syncEngine = syncEngine ?? throw new ArgumentNullException(nameof(syncEngine));

        LoadFoldersCommand = ReactiveCommand.CreateFromTask(LoadFoldersAsync);
        LoadChildrenCommand = ReactiveCommand.CreateFromTask<OneDriveFolderNode>(LoadChildrenAsync);
        ToggleSelectionCommand = ReactiveCommand.Create<OneDriveFolderNode>(ToggleSelection);
        ClearSelectionsCommand = ReactiveCommand.Create(ClearSelections);
        StartSyncCommand = ReactiveCommand.CreateFromTask(StartSyncAsync,
            this.WhenAnyValue(x => x.SelectedAccountId, x => x.IsSyncing,
                (accountId, isSyncing) => !string.IsNullOrEmpty(accountId) && !isSyncing));
        StopSyncCommand = ReactiveCommand.CreateFromTask(StopSyncAsync,
            this.WhenAnyValue(x => x.IsSyncing));

        this.WhenAnyValue(x => x.SelectedAccountId)
            .Subscribe(_ => LoadFoldersCommand.Execute().Subscribe())
            .DisposeWith(_disposables);

        // Subscribe to sync progress updates
        _syncEngine.Progress
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(state =>
            {
                SyncState = state;
                this.RaisePropertyChanged(nameof(IsSyncing));
                this.RaisePropertyChanged(nameof(ProgressPercentage));
                this.RaisePropertyChanged(nameof(ProgressText));
            })
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Loads root folders for the currently selected account.
    /// </summary>
    private async Task LoadFoldersAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(SelectedAccountId))
        {
            RootFolders.Clear();
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var folders = await _folderTreeService.GetRootFoldersAsync(SelectedAccountId, cancellationToken);

            RootFolders.Clear();
            foreach (var folder in folders)
            {
                RootFolders.Add(folder);
            }

            // Load saved selections from database
            await _selectionService.LoadSelectionsFromDatabaseAsync(SelectedAccountId, [.. RootFolders], cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load folders: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads child folders for a specific folder node.
    /// </summary>
    /// <param name="folder">The folder whose children should be loaded.</param>
    private async Task LoadChildrenAsync(OneDriveFolderNode folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if (folder.ChildrenLoaded || string.IsNullOrEmpty(SelectedAccountId))
            return;

        try
        {
            var children = await _folderTreeService.GetChildFoldersAsync(SelectedAccountId, folder.Id, cancellationToken);

            folder.Children.Clear();
            foreach (var child in children)
            {
                // Inherit parent's selection state for new children
                if (folder.SelectionState == SelectionState.Checked)
                {
                    child.SelectionState = SelectionState.Checked;
                    child.IsSelected = true;
                }

                folder.Children.Add(child);
            }

            folder.ChildrenLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load child folders: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggles the selection state of a folder.
    /// </summary>
    /// <param name="folder">The folder to toggle.</param>
    private void ToggleSelection(OneDriveFolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var newState = folder.SelectionState switch
        {
            SelectionState.Unchecked => true,
            SelectionState.Checked => false,
            SelectionState.Indeterminate => true,
            _ => true
        };

        _selectionService.SetSelection(folder, newState);
        _selectionService.UpdateParentStates(folder, [.. RootFolders]);

        // Save selections to database (fire and forget for UI responsiveness)
        if (!string.IsNullOrEmpty(SelectedAccountId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _selectionService.SaveSelectionsToDatabaseAsync(SelectedAccountId, [.. RootFolders]);
                }
                catch
                {
                    // Silently ignore persistence errors to avoid disrupting UI
                }
            });
        }
    }

    /// <summary>
    /// Clears all folder selections.
    /// </summary>
    private void ClearSelections()
    {
        _selectionService.ClearAllSelections([.. RootFolders]);

        // Clear selections from database (fire and forget for UI responsiveness)
        if (!string.IsNullOrEmpty(SelectedAccountId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _selectionService.SaveSelectionsToDatabaseAsync(SelectedAccountId, [.. RootFolders]);
                }
                catch
                {
                    // Silently ignore persistence errors to avoid disrupting UI
                }
            });
        }
    }

    /// <summary>
    /// Gets all selected folders.
    /// </summary>
    /// <returns>List of selected folder nodes.</returns>
    public List<OneDriveFolderNode> GetSelectedFolders()
    {
        return _selectionService.GetSelectedFolders([.. RootFolders]);
    }

    /// <summary>
    /// Starts file synchronization for the selected account.
    /// </summary>
    private async Task StartSyncAsync()
    {
        if (string.IsNullOrEmpty(SelectedAccountId))
        {
            return;
        }

        try
        {
            await _syncEngine.StartSyncAsync(SelectedAccountId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Sync failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Stops any ongoing synchronization.
    /// </summary>
    private async Task StopSyncAsync()
    {
        await _syncEngine.StopSyncAsync();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
