using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.OneDriveServices;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.Syncronisation;

/// <summary>
///     ViewModel for the sync tree view, managing folder hierarchy and selection.
/// </summary>
public sealed class SyncTreeViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IFolderTreeService _folderTreeService;
    private readonly ISyncSelectionService _selectionService;
    private readonly ISyncEngine _syncEngine;
    private readonly IDebugLogger _debugLogger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SyncTreeViewModel" /> class.
    /// </summary>
    /// <param name="folderTreeService">Service for loading folder hierarchies.</param>
    /// <param name="selectionService">Service for managing selection state.</param>
    /// <param name="syncEngine">Service for file synchronization.</param>
    /// <param name="debugLogger">Service for logging debug information.</param>
    public SyncTreeViewModel(IFolderTreeService folderTreeService, ISyncSelectionService selectionService, ISyncEngine syncEngine, IDebugLogger debugLogger)
    {
        _folderTreeService = folderTreeService ?? throw new ArgumentNullException(nameof(folderTreeService));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _syncEngine = syncEngine ?? throw new ArgumentNullException(nameof(syncEngine));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));

        LoadFoldersCommand = ReactiveCommand.CreateFromTask(LoadFoldersAsync);
        LoadChildrenCommand = ReactiveCommand.CreateFromTask<OneDriveFolderNode>(LoadChildrenAsync);
        ToggleSelectionCommand = ReactiveCommand.Create<OneDriveFolderNode>(ToggleSelection);
        ClearSelectionsCommand = ReactiveCommand.Create(ClearSelections);
        StartSyncCommand = ReactiveCommand.CreateFromTask(StartSyncAsync,
            this.WhenAnyValue(x => x.SelectedAccountId, x => x.IsSyncing,
                (accountId, isSyncing) => !string.IsNullOrEmpty(accountId) && !isSyncing));
        CancelSyncCommand = ReactiveCommand.CreateFromTask(CancelSyncAsync,
            this.WhenAnyValue(x => x.IsSyncing));

        // Only allow opening if syncing and not already open
        OpenSyncProgressCommand = ReactiveCommand.Create(
            () => { IsSyncProgressOpen = true; },
            this.WhenAnyValue(x => x.IsSyncing, x => x.IsSyncProgressOpen, (syncing, open) => syncing && !open));

        _ = this.WhenAnyValue(x => x.SelectedAccountId)
            .Subscribe(_ => LoadFoldersCommand.Execute().Subscribe())
            .DisposeWith(_disposables);

        // Subscribe to sync progress updates
        _ = _syncEngine.Progress
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(state =>
            {
                SyncState = state;
                this.RaisePropertyChanged(nameof(IsSyncing));
                this.RaisePropertyChanged(nameof(IsPaused));
                this.RaisePropertyChanged(nameof(ProgressPercentage));
                this.RaisePropertyChanged(nameof(ProgressText));
            })
            .DisposeWith(_disposables);
    }

    /// <summary>
    ///     Gets the estimated seconds remaining for sync (ETA).
    /// </summary>
    public double? EstimatedSecondsRemaining => SyncState.EstimatedSecondsRemaining;

    /// <summary>
    ///     Gets the current sync speed in megabytes per second.
    /// </summary>
    public double MegabytesPerSecond => SyncState.MegabytesPerSecond;

    /// <summary>
    ///     Gets or sets a value indicating whether the SyncProgressView is open.
    /// </summary>
    public bool IsSyncProgressOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Command to open the SyncProgressView.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSyncProgressCommand { get; }

    /// <summary>
    ///     Gets or sets the currently selected account ID.
    /// </summary>
    public string? SelectedAccountId
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets the root-level folders for the selected account.
    /// </summary>
    public ObservableCollection<OneDriveFolderNode> Folders
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    /// <summary>
    ///     Gets a value indicating whether folders are currently being loaded.
    /// </summary>
    public bool IsLoading
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets the error message if loading fails.
    /// </summary>
    public string? ErrorMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets the current sync state.
    /// </summary>
    public SyncState SyncState
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = SyncState.CreateInitial("");

    /// <summary>
    ///     Gets a value indicating whether sync is currently running.
    /// </summary>
    public bool IsSyncing => IsRunning(SyncState);

    /// <summary>
    ///     Gets a value indicating whether sync is paused.
    /// </summary>
    public bool IsPaused => SyncState.Status == SyncStatus.Paused;

    /// <summary>
    ///     Gets sync progress as percentage (0-100).
    /// </summary>
    public double ProgressPercentage => SyncState.TotalFiles > 0
        ? (double)SyncState.CompletedFiles / SyncState.TotalFiles * 100
        : 0;

    /// <summary>
    ///     Gets sync progress text for display.
    /// </summary>
    public string ProgressText => SyncState.Status switch
    {
        SyncStatus.Idle => "Ready to sync",
        SyncStatus.Running when SyncState.TotalFiles == 0 => "Scanning for changes...",
        SyncStatus.Running => $"Syncing {SyncState.CompletedFiles} of {SyncState.TotalFiles} files",
        SyncStatus.Completed when SyncState.TotalFiles == 0 => "No changes to sync",
        SyncStatus.Completed => $"Sync completed - {SyncState.TotalFiles} files",
        SyncStatus.Paused => "Sync paused",
        SyncStatus.Failed => "Sync failed",
        SyncStatus.Queued => throw new NotImplementedException(),
        SyncStatus.InitialDeltaSync => $"Starting the initial Delta Sync...processing page: {SyncState.TotalFiles}",
        SyncStatus.IncrementalDeltaSync => "Starting the incremental Delta Sync...this could take some time...",
        _ => string.Empty
    };

    /// <summary>
    ///     Gets the result message from the last sync operation.
    /// </summary>
    public string? LastSyncResult
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Command to load root folders for the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFoldersCommand { get; }

    /// <summary>
    ///     Command to load child folders when a node is expanded.
    /// </summary>
    public ReactiveCommand<OneDriveFolderNode, Unit> LoadChildrenCommand { get; }

    /// <summary>
    ///     Command to toggle folder selection state.
    /// </summary>
    public ReactiveCommand<OneDriveFolderNode, Unit> ToggleSelectionCommand { get; }

    /// <summary>
    ///     Command to clear all selections.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearSelectionsCommand { get; }

    /// <summary>
    ///     Command to start synchronization.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSyncCommand { get; }

    /// <summary>
    ///     Command to cancel synchronization.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelSyncCommand { get; }

    /// <inheritdoc />
    public void Dispose() => _disposables.Dispose();

    /// <summary>
    ///     Gets all selected folders.
    /// </summary>
    /// <returns>List of selected folder nodes.</returns>
    public List<OneDriveFolderNode> GetSelectedFolders() => _selectionService.GetSelectedFolders([.. Folders]);

    private async Task LoadFoldersAsync(CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrEmpty(SelectedAccountId))
        {
            Folders.Clear();
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            IList<OneDriveFolderNode> folderList = await _selectionService.LoadSelectionsFromDatabaseAsync(SelectedAccountId, cancellationToken);

            Folders.Clear();

            var nodesByPath = folderList
                .ToDictionary(
                    f => Normalize(f.Path),
                    f => new OneDriveFolderNode(f.DriveItemId, f.Name, f.Path, f.ParentId, f.IsFolder, f.IsSelected)
                );

            var roots = new List<OneDriveFolderNode>();

            foreach(OneDriveFolderNode? node in nodesByPath.Values)
            {
                var path = Normalize(node.Path);
                var parentPath = GetParentPath(path);

                if(parentPath is null || !nodesByPath.TryGetValue(parentPath, out OneDriveFolderNode? parent))
                {
                    roots.Add(node);
                    continue;
                }

                parent.Children.Add(node);
            }

            Folders = new ObservableCollection<OneDriveFolderNode>(roots);
        }
        catch(Exception ex)
        {
            ErrorMessage = $"Failed to load folders: {ex.GetBaseException().Message}";
            await _debugLogger.LogErrorAsync("SyncTreeViewModel.LoadFoldersAsync", SelectedAccountId, $"Loading folders for account {SelectedAccountId}. {ErrorMessage}", cancellationToken: cancellationToken);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadChildrenAsync(OneDriveFolderNode folder, CancellationToken cancellationToken = default)
    {
        if(folder.ChildrenLoaded || string.IsNullOrEmpty(SelectedAccountId))
            return;

        try
        {
            folder.IsLoading = true;
            folder.Children.Clear();

            IReadOnlyList<OneDriveFolderNode> children = await _folderTreeService.GetChildFoldersAsync(SelectedAccountId, folder.DriveItemId, folder.IsSelected, cancellationToken);
            foreach(OneDriveFolderNode child in children)
                folder.Children.Add(child);

            await _selectionService.LoadSelectionsFromDatabaseAsync(SelectedAccountId, [.. folder.Children], cancellationToken);

            _selectionService.UpdateParentState(folder);

            folder.ChildrenLoaded = true;
        }
        catch(Exception ex)
        {
            ErrorMessage = $"Failed to load child folders: {ex.Message}";
        }
        finally
        {
            folder.IsLoading = false;
        }
    }

    private void ToggleSelection(OneDriveFolderNode folder)
    {
        var newState = folder.SelectionState switch
        {
            SelectionState.Unchecked => true,
            SelectionState.Checked => false,
            SelectionState.Indeterminate => true,
            _ => true
        };

        _selectionService.SetSelection(folder, newState);
        _selectionService.UpdateParentStates(folder, [.. Folders]);

        if(!string.IsNullOrEmpty(SelectedAccountId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _selectionService.SaveSelectionsToDatabaseAsync(SelectedAccountId, [.. Folders]);
                }
                catch(Exception ex)
                {
                    await _debugLogger.LogErrorAsync("SyncTreeViewModel.ToggleSelection", SelectedAccountId, $"Saving selections for account {SelectedAccountId}. {ex.Message}", ex);
                }
            });
        }
    }

    private bool IsRunning(SyncState syncState) => syncState.Status is SyncStatus.Running or SyncStatus.InitialDeltaSync or SyncStatus.IncrementalDeltaSync;

    private void ClearSelections()
    {
        _selectionService.ClearAllSelections([.. Folders]);

        if(!string.IsNullOrEmpty(SelectedAccountId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _selectionService.SaveSelectionsToDatabaseAsync(SelectedAccountId, [.. Folders]);
                }
                catch
                {
                    // Silently ignore persistence errors to avoid disrupting UI
                }
            });
        }
    }

    private async Task StartSyncAsync(CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.UI.SyncTreeViewModel.StartSync, SelectedAccountId ?? AdminAccountMetadata.AccountId, CancellationToken.None);
        if(string.IsNullOrEmpty(SelectedAccountId))
            return;

        try
        {
            LastSyncResult = null;
            await _syncEngine.StartSyncAsync(SelectedAccountId);

            if(SyncState.Status == SyncStatus.Completed)
            {
                var totalChanges = SyncState.TotalFiles + SyncState.FilesDeleted;

                LastSyncResult = totalChanges == 0 ? "✓ Sync complete: No changes detected" :
                    totalChanges > 1 ? $"✓ Sync complete: {totalChanges} change(s) synchronized" : $"✓ Sync complete: {totalChanges} change synchronized";
            }

            await LoadFoldersAsync(cancellationToken);
        }
        catch(OperationCanceledException)
        {
            // Sync was cancelled - this is expected, don't show error
            // The cancel command already set the LastSyncResult message
        }
        catch(Exception ex)
        {
            ErrorMessage = $"Sync failed: {ex.Message}";
            LastSyncResult = null;
        }
    }

    private async Task CancelSyncAsync()
    {
        await _syncEngine.StopSyncAsync();
        LastSyncResult = "Sync cancelled";
    }

    private static string Normalize(string path)
    {
        if(string.IsNullOrWhiteSpace(path))
            return "/";

        path = path.Trim()
                   .Replace("\\", "/")
                   .TrimEnd('/');

        return path.StartsWith('/') ? path : "/" + path;
    }

    private static string? GetParentPath(string path)
    {
        if(path == "/")
            return null;

        var lastSlash = path.LastIndexOf('/');
        return lastSlash <= 0 ? "/" : path[..lastSlash];
    }
}
