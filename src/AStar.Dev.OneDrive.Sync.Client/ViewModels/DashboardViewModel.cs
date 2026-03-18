using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class DashboardViewModel(SyncScheduler scheduler) : ObservableObject
{
    // ── Account sections ──────────────────────────────────────────────────

    public ObservableCollection<DashboardAccountViewModel> AccountSections { get; } = [];

    // ── Global stats ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    private int _totalAccounts;

    [ObservableProperty] private int    _totalFolders;
    [ObservableProperty] private int    _totalConflicts;
    [ObservableProperty] private string _lastSyncText   = "Never";
    [ObservableProperty] private bool   _anyErrors;
    [ObservableProperty] private bool   _anySyncing;

    public bool HasAccounts => TotalAccounts > 0;

    public string OverallStatusText => (AnySyncing, AnyErrors, TotalConflicts) switch
    {
        (true,  _,    _) => "Syncing ...",
        (_,     true, _) => "Error",
        (_,     _, > 0) => $"{TotalConflicts} conflict{ (TotalConflicts == 1 ? "" : "s")}",
        _                => "All synced"
    };

    // ── Public API ────────────────────────────────────────────────────────

    public void AddAccount(OneDriveAccount account)
    {
        if (AccountSections.Any(s => s.AccountId == account.Id)) return;

        var section = new DashboardAccountViewModel(account, scheduler);
        AccountSections.Add(section);
        RecalculateGlobals();
    }

    public void RemoveAccount(string accountId)
    {
        DashboardAccountViewModel? section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if (section is null) return;
        _ = AccountSections.Remove(section);
        RecalculateGlobals();
    }

    public void UpdateAccountSyncState(
        string    accountId,
        SyncState state,
        int       conflicts)
    {
        DashboardAccountViewModel? section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        section?.UpdateSyncState(state, conflicts);
        RecalculateGlobals();
    }

    public void AddActivityItem(ActivityItemViewModel item)
    {
        DashboardAccountViewModel? section = AccountSections
            .FirstOrDefault(s => s.AccountId == item.AccountId);
        section?.AddRecentActivity(item);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void RecalculateGlobals()
    {
        TotalAccounts  = AccountSections.Count;
        TotalFolders   = AccountSections.Sum(s => s.FolderCount);
        TotalConflicts = AccountSections.Sum(s => s.ConflictCount);
        AnyErrors      = AccountSections.Any(s => s.SyncState == SyncState.Error);
        AnySyncing     = AccountSections.Any(s => s.SyncState == SyncState.Syncing);

        DashboardAccountViewModel? mostRecent = AccountSections
            .Where(s => s.LastSyncText != "Never synced")
            .FirstOrDefault();

        LastSyncText = mostRecent?.LastSyncText ?? "Never";
        OnPropertyChanged(nameof(OverallStatusText));
    }
}
