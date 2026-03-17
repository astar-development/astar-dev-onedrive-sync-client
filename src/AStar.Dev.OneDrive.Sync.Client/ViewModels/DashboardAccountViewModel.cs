using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class DashboardAccountViewModel : ObservableObject
{
    private readonly OneDriveAccount _account;
    private readonly SyncScheduler   _scheduler;

    // ── Identity ──────────────────────────────────────────────────────────

    public string AccountId   => _account.Id;
    public string DisplayName => _account.DisplayName;
    public string Email       => _account.Email;
    public string AccentHex   => AccountCardViewModel.PaletteHex(_account.AccentIndex);
    public Avalonia.Media.Color AccentColor =>
        Avalonia.Media.Color.Parse(AccentHex);

    // ── Storage ───────────────────────────────────────────────────────────

    public long   QuotaTotal    => _account.QuotaTotal;
    public long   QuotaUsed     => _account.QuotaUsed;
    public double StorageFraction => QuotaTotal > 0
        ? Math.Clamp((double)QuotaUsed / QuotaTotal, 0, 1)
        : 0;

    public string StorageText => QuotaTotal > 0
        ? $"{FormatBytes(QuotaUsed)} / {FormatBytes(QuotaTotal)}"
        : "Unknown";

    // ── Sync state ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    private SyncState _syncState = SyncState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    private int _conflictCount;

    [ObservableProperty] private string _lastSyncText = "Never synced";
    [ObservableProperty] private int    _folderCount;
    [ObservableProperty] private bool   _isSyncing;

    public bool   IsHealthy   => SyncState is SyncState.Idle && ConflictCount == 0;
    public string StatusLabel => (SyncState, ConflictCount) switch
    {
        (SyncState.Syncing,  _) => "Syncing\u2026",
        (SyncState.Error,    _) => "Error",
        (_, > 0)                => $"{ConflictCount} conflict{(ConflictCount == 1 ? "" : "s")}",
        (SyncState.Pending,  _) => "Pending",
        _                       => "Synced"
    };

    // ── Expansion ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderGlyph))]
    private bool _isExpanded = true;

    public string ExpanderGlyph => IsExpanded ? "\u25BE" : "\u25B8";

    // ── Recent activity (last 3 items) ────────────────────────────────────

    public ObservableCollection<ActivityItemViewModel> RecentActivity { get; } = [];

    // ── Construction ──────────────────────────────────────────────────────

    public DashboardAccountViewModel(
        OneDriveAccount account,
        SyncScheduler   scheduler)
    {
        _account   = account;
        _scheduler = scheduler;
        _folderCount = account.SelectedFolderIds.Count;
        UpdateLastSyncText();
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        IsSyncing = true;
        SyncState = SyncState.Syncing;
        try
        {
            await _scheduler.TriggerAccountAsync(_account);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    // ── Public update API (called by DashboardViewModel) ─────────────────

    public void UpdateSyncState(SyncState state, int conflicts) => Dispatcher.UIThread.Post(() =>
                                                                        {
                                                                            SyncState = state;
                                                                            ConflictCount = conflicts;
                                                                            IsSyncing = state == SyncState.Syncing;
                                                                            UpdateLastSyncText();
                                                                        });

    public void AddRecentActivity(ActivityItemViewModel item) => Dispatcher.UIThread.Post(() =>
                                                                      {
                                                                          RecentActivity.Insert(0, item);
                                                                          while (RecentActivity.Count > 3)
                                                                              RecentActivity.RemoveAt(RecentActivity.Count - 1);
                                                                      });

    // ── Private helpers ───────────────────────────────────────────────────

    private void UpdateLastSyncText() =>
        LastSyncText = _account.LastSyncedAt is null
            ? "Never synced"
            : (DateTimeOffset.UtcNow - _account.LastSyncedAt.Value) switch
            {
                { TotalSeconds: < 60  }    => "Just now",
                { TotalMinutes: < 60  } td => $"{(int)td.TotalMinutes}m ago",
                { TotalHours:   < 24  } td => $"{(int)td.TotalHours}h ago",
                { TotalDays:    < 2   }    => "Yesterday",
                var td                     => $"{(int)td.TotalDays}d ago"
            };

    private static string FormatBytes(long bytes) => bytes switch
    {
        0                    => "0 B",
        < 1024               => $"{bytes} B",
        < 1024 * 1024        => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _                    => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
