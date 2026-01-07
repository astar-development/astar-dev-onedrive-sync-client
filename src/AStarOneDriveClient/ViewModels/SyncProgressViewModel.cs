using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Avoid string interpolation in logging - Acceptable for ViewModels

namespace AStarOneDriveClient.ViewModels;

/// <summary>
/// ViewModel for displaying real-time sync progress with pause/resume controls.
/// </summary>
public sealed class SyncProgressViewModel : ReactiveObject, IDisposable
{
    private readonly ISyncEngine _syncEngine;
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<SyncProgressViewModel> _logger;

    /// <summary>
    /// Gets the account ID for this sync progress.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets or sets the current sync progress state.
    /// </summary>
    public SyncState? CurrentProgress { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether sync is currently running.
    /// </summary>
    public bool IsSyncing { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    /// <summary>
    /// Gets or sets the status message to display.
    /// </summary>
    public string StatusMessage { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Ready to sync";

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage
    {
        get
        {
            return CurrentProgress is null || CurrentProgress.TotalFiles == 0
                ? 0
                : (double)CurrentProgress.CompletedFiles / CurrentProgress.TotalFiles * 100;
        }
    }

    /// <summary>
    /// Gets a formatted string showing completed/total files.
    /// </summary>
    public string FilesProgressText => CurrentProgress is null ? "No files" : $"{CurrentProgress.CompletedFiles} of {CurrentProgress.TotalFiles} files";

    /// <summary>
    /// Gets a formatted string showing upload/download counts.
    /// </summary>
    public string TransferDetailsText
    {
        get
        {
            if (CurrentProgress is null)
            {
                return string.Empty;
            }

            var parts = new List<string>
            {
                $"↑ {CurrentProgress.FilesUploading} uploading  ↓ {CurrentProgress.FilesDownloading} downloading"
            };

            // Add transfer speed if available
            if (CurrentProgress.MegabytesPerSecond > 0.01)
            {
                parts.Add($"{CurrentProgress.MegabytesPerSecond:F2} MB/s");
            }

            // Add ETA if available
            if (CurrentProgress.EstimatedSecondsRemaining.HasValue)
            {
                var eta = FormatTimeRemaining(CurrentProgress.EstimatedSecondsRemaining.Value);
                parts.Add($"ETA: {eta}");
            }

            return string.Join("  •  ", parts);
        }
    }

    /// <summary>
    /// Gets a formatted string showing conflicts detected.
    /// </summary>
    public string ConflictsText
    {
        get
        {
            return CurrentProgress is null || CurrentProgress.ConflictsDetected == 0
                ? string.Empty
                : $"⚠ {CurrentProgress.ConflictsDetected} conflict(s) detected";
        }
    }

    /// <summary>
    /// Gets a value indicating whether conflicts were detected.
    /// </summary>
    public bool HasConflicts => CurrentProgress?.ConflictsDetected > 0;

    /// <summary>
    /// Gets the command to start sync.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSyncCommand { get; }

    /// <summary>
    /// Gets the command to pause sync.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PauseSyncCommand { get; }

    /// <summary>
    /// Gets the command to view conflicts.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewConflictsCommand { get; }

    /// <summary>
    /// Gets the command to close the sync progress view.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncProgressViewModel"/>.
    /// </summary>
    /// <param name="accountId">The account ID to sync.</param>
    /// <param name="syncEngine">The sync engine service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="accountId"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is <c>null</c>.</exception>
    public SyncProgressViewModel(
        string accountId,
        ISyncEngine syncEngine,
        ILogger<SyncProgressViewModel> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(syncEngine);
        ArgumentNullException.ThrowIfNull(logger);

        AccountId = accountId;
        _syncEngine = syncEngine;
        _logger = logger;

        var canStart = this.WhenAnyValue(x => x.IsSyncing, isSyncing => !isSyncing);
        var canPause = this.WhenAnyValue(x => x.IsSyncing);
        var hasConflicts = this.WhenAnyValue(x => x.HasConflicts);

        StartSyncCommand = ReactiveCommand.CreateFromTask(StartSyncAsync, canStart);
        PauseSyncCommand = ReactiveCommand.CreateFromTask(PauseSyncAsync, canPause);
        ViewConflictsCommand = ReactiveCommand.Create(OnViewConflicts, hasConflicts);
        CloseCommand = ReactiveCommand.Create(() => { /* Handled by MainWindowViewModel */ });

        // Subscribe to sync progress updates
        _syncEngine.Progress
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(progress =>
            {
                CurrentProgress = progress;

                // Update IsSyncing based on sync status
                IsSyncing = progress.Status == SyncStatus.Running;

                UpdateStatusMessage();
                this.RaisePropertyChanged(nameof(ProgressPercentage));
                this.RaisePropertyChanged(nameof(FilesProgressText));
                this.RaisePropertyChanged(nameof(TransferDetailsText));
                this.RaisePropertyChanged(nameof(ConflictsText));
                this.RaisePropertyChanged(nameof(HasConflicts));

                _logger.LogDebug(
                    "Progress update: {Completed}/{Total} files, {Conflicts} conflicts",
                    progress.CompletedFiles,
                    progress.TotalFiles,
                    progress.ConflictsDetected);
            })
            .DisposeWith(_disposables);

        _logger.LogInformation("SyncProgressViewModel initialized for account {AccountId}", accountId);
    }

    /// <summary>
    /// Starts the sync operation.
    /// </summary>
    private async Task StartSyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsSyncing = true;
            StatusMessage = "Starting sync...";
            _logger.LogInformation("Starting sync for account {AccountId}", AccountId);

            await _syncEngine.StartSyncAsync(AccountId, cancellationToken);

            StatusMessage = "Sync completed successfully";
            _logger.LogInformation("Sync completed for account {AccountId}", AccountId);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Sync paused";
            _logger.LogInformation("Sync paused for account {AccountId}", AccountId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
            _logger.LogError(ex, "Sync failed for account {AccountId}", AccountId);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    /// <summary>
    /// Pauses the sync operation.
    /// </summary>
    private async Task PauseSyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            StatusMessage = "Pausing sync...";
            _logger.LogInformation("Pausing sync for account {AccountId}", AccountId);

            await _syncEngine.StopSyncAsync();

            StatusMessage = "Sync paused";
            IsSyncing = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to pause sync: {ex.Message}";
            _logger.LogError(ex, "Failed to pause sync for account {AccountId}", AccountId);
        }
    }

    /// <summary>
    /// Handles the view conflicts command.
    /// </summary>
    private void OnViewConflicts() => _logger.LogInformation("User requested to view conflicts for account {AccountId}", AccountId);// Navigation logic would go here in a real implementation// e.g., NavigationService.NavigateTo<ConflictResolutionViewModel>(_accountId);

    /// <summary>
    /// Updates the status message based on current progress.
    /// </summary>
    private void UpdateStatusMessage()
    {
        if (CurrentProgress is null)
        {
            StatusMessage = "Ready to sync";
            return;
        }

        if (IsSyncing)
        {
            if (CurrentProgress.TotalFiles == 0)
            {
                StatusMessage = "Scanning for changes...";
            }
            else
            {
                StatusMessage = CurrentProgress.CompletedFiles == CurrentProgress.TotalFiles
                    ? "Finalizing sync..."
                    : $"Syncing {CurrentProgress.CompletedFiles} of {CurrentProgress.TotalFiles} files...";
            }
        }
        else if (CurrentProgress.CompletedFiles == CurrentProgress.TotalFiles && CurrentProgress.TotalFiles > 0)
        {
            StatusMessage = $"Sync completed - {CurrentProgress.TotalFiles} file(s) processed";
        }
    }

    /// <summary>
    /// Refreshes the conflict count from the database and updates CurrentProgress.
    /// </summary>
    /// <remarks>
    /// Call this method after resolving conflicts to update the UI with the current conflict count.
    /// </remarks>
    public async Task RefreshConflictCountAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentProgress is null)
        {
            return;
        }

        var conflicts = await _syncEngine.GetConflictsAsync(AccountId, cancellationToken);
        var conflictCount = conflicts.Count;

        CurrentProgress = CurrentProgress with
        {
            ConflictsDetected = conflictCount
        };

        this.RaisePropertyChanged(nameof(ConflictsText));
        this.RaisePropertyChanged(nameof(HasConflicts));

        _logger.LogDebug("Refreshed conflict count for account {AccountId}: {Count} conflicts", AccountId, conflictCount);
    }

    /// <summary>
    /// Formats time remaining in seconds to a human-readable string.
    /// </summary>
    /// <param name="seconds">Total seconds remaining.</param>
    /// <returns>Formatted time string (e.g., "2h 15m", "45m", "30s").</returns>
    private static string FormatTimeRemaining(int seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds}s";
        }

        if (seconds < 3600)
        {
            var minutes = seconds / 60;
            return $"{minutes}m";
        }

        var hours = seconds / 3600;
        var remainingMinutes = seconds % 3600 / 60;
        return remainingMinutes > 0 ? $"{hours}h {remainingMinutes}m" : $"{hours}h";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("SyncProgressViewModel disposed for account {AccountId}", AccountId);
    }
}
