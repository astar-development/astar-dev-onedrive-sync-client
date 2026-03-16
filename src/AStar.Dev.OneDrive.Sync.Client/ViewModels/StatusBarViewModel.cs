using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public enum SyncState { Idle, Syncing, Pending, Conflict, Error }

/// <summary>
/// Drives the bottom status bar.  Bound to the active account only.
/// Updated by the sync engine via property changes.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    // ── Active account ────────────────────────────────────────────────────
    [ObservableProperty] private string  _accountDisplayName = string.Empty;
    [ObservableProperty] private string  _accountEmail       = string.Empty;
    [ObservableProperty] private bool    _hasAccount;

    // ── Sync state ────────────────────────────────────────────────────────
    [ObservableProperty] private SyncState _syncState = SyncState.Idle;
    [ObservableProperty] private int       _pendingCount;
    [ObservableProperty] private int       _conflictCount;
    [ObservableProperty] private string    _lastSyncText  = string.Empty;
    [ObservableProperty] private bool      _isSyncing;

    // ── Storage ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _storageUsedText = string.Empty;

    // ── Conflict policy (display only — editing is in Settings) ──────────
    [ObservableProperty] private string _conflictPolicyText = string.Empty;

    // ── Progress (shown only while syncing) ──────────────────────────────
    [ObservableProperty] private double _syncProgress;   // 0–1, NaN = indeterminate

    // ── Derived label (bound to a TextBlock in the status bar) ───────────
    public string StatusLabel => SyncState switch
    {
        SyncState.Syncing  => "Syncing\u2026",
        SyncState.Pending  => $"{PendingCount} pending",
        SyncState.Conflict => ConflictCount == 1 ? "1 conflict" : $"{ConflictCount} conflicts",
        SyncState.Error    => "Error",
        _                  => "Synced"
    };

    partial void OnSyncStateChanged(SyncState value)   => OnPropertyChanged(nameof(StatusLabel));
    partial void OnPendingCountChanged(int value)       => OnPropertyChanged(nameof(StatusLabel));
    partial void OnConflictCountChanged(int value)      => OnPropertyChanged(nameof(StatusLabel));
}
