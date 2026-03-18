using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class ActivityViewModel(ISyncService syncService, ISyncRepository syncRepository) : ObservableObject
{
    private string? _activeAccountId;
    private string _activeAccountEmail = string.Empty;

    // ── Tab ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLogTabActive))]
    [NotifyPropertyChangedFor(nameof(IsConflictsTabActive))]
    private ActivityTab _activeTab = ActivityTab.Log;

    public bool IsLogTabActive => ActiveTab == ActivityTab.Log;
    public bool IsConflictsTabActive => ActiveTab == ActivityTab.Conflicts;

    [RelayCommand]
    private void SwitchTab(ActivityTab tab) => ActiveTab = tab;

    // ── Activity log ──────────────────────────────────────────────────────

    public ObservableCollection<ActivityItemViewModel> LogItems { get; } = [];
    public ObservableCollection<ActivityItemViewModel> FilteredLog { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogItems))]
    private int _logItemCount;

    public bool HasLogItems => LogItemCount > 0;

    [ObservableProperty] private ActivityItemType? _activeFilter;

    // ── Conflicts ─────────────────────────────────────────────────────────

    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflicts))]
    [NotifyPropertyChangedFor(nameof(ConflictBadgeText))]
    private int _conflictCount;

    public bool HasConflicts => ConflictCount > 0;
    public string ConflictBadgeText => ConflictCount > 0 ? ConflictCount.ToString() : string.Empty;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by MainWindowViewModel when the active account changes.
    /// Loads persisted conflicts for the account.
    /// </summary>
    public async Task SetActiveAccountAsync(string accountId, string accountEmail)
    {
        _activeAccountId = accountId;
        _activeAccountEmail = accountEmail;

        Conflicts.Clear();
        FilteredLog.Clear();

        // Reload persisted conflicts for this account
        List<SyncConflictEntity> persistedConflicts = await syncRepository
            .GetPendingConflictsAsync(accountId);

        foreach(SyncConflictEntity entity in persistedConflicts)
        {
            var model = new SyncConflict
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                FolderId = entity.FolderId,
                RemoteItemId = entity.RemoteItemId,
                RelativePath = entity.RelativePath,
                LocalPath = entity.LocalPath,
                LocalModified = entity.LocalModified,
                RemoteModified = entity.RemoteModified,
                LocalSize = entity.LocalSize,
                RemoteSize = entity.RemoteSize,
                DetectedAt = entity.DetectedAt
            };

            AddConflict(model);
        }

        // Filter log to this account
        RebuildFilteredLog();
        ConflictCount = Conflicts.Count;
    }

    /// <summary>Called by MainWindowViewModel when a sync job completes.</summary>
    public void AddActivityItem(ActivityItemViewModel item) => Dispatcher.UIThread.Post(() =>
                                                                    {
                                                                        LogItems.Insert(0, item);

                                                                        // Keep log to 500 items max
                                                                        while(LogItems.Count > 500)
                                                                            LogItems.RemoveAt(LogItems.Count - 1);

                                                                        LogItemCount = LogItems.Count;
                                                                        RebuildFilteredLog();
                                                                    });

    /// <summary>Called by MainWindowViewModel when a new conflict is detected.</summary>
    public void AddConflictItem(SyncConflict conflict) => Dispatcher.UIThread.Post(() =>
                                                               {
                                                                   if(Conflicts.Any(c => c.Id == conflict.Id))
                                                                       return;
                                                                   AddConflict(conflict);
                                                                   ConflictCount = Conflicts.Count;

                                                                   // Switch to conflicts tab automatically
                                                                   ActiveTab = ActivityTab.Conflicts;
                                                               });

    // ── Filter commands ───────────────────────────────────────────────────

    [RelayCommand]
    private void SetFilter(ActivityItemType? filter)
    {
        ActiveFilter = filter;
        RebuildFilteredLog();
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogItems.Clear();
        FilteredLog.Clear();
        LogItemCount = 0;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void AddConflict(SyncConflict conflict)
    {
        var vm = new ConflictItemViewModel(conflict, syncService);
        vm.Resolved += (_, c) =>
        {
            _ = Conflicts.Remove(c);
            ConflictCount = Conflicts.Count;
        };
        Conflicts.Add(vm);
    }

    private void RebuildFilteredLog()
    {
        FilteredLog.Clear();

        IEnumerable<ActivityItemViewModel> query = LogItems
            .Where(i => _activeAccountId is null || i.AccountId == _activeAccountId);

        if(ActiveFilter.HasValue)
            query = query.Where(i => i.Type == ActiveFilter.Value);

        foreach(ActivityItemViewModel? item in query)
            FilteredLog.Add(item);

        LogItemCount = FilteredLog.Count;
    }
}
