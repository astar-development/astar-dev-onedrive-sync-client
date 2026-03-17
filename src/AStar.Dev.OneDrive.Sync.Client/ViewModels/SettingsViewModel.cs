using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class AccountSyncSettingsViewModel(
    OneDriveAccount    account,
    IAccountRepository repository) : ObservableObject
{
    public string AccountId    => account.Id;
    public string Email        => account.Email;
    public string DisplayName  => account.DisplayName;
    public string AccentHex    => AccountCardViewModel.PaletteHex(account.AccentIndex);

    [ObservableProperty] private string         _localSyncPath  = account.LocalSyncPath;
    [ObservableProperty] private ConflictPolicy _conflictPolicy = account.ConflictPolicy;

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new(ConflictPolicy.Ignore,        "Ignore",          "Skip conflicts — leave both unchanged"),
        new(ConflictPolicy.KeepBoth,      "Keep both",       "Rename local, keep remote"),
        new(ConflictPolicy.LastWriteWins, "Last write wins", "Most recently modified wins"),
        new(ConflictPolicy.LocalWins,     "Local wins",      "Local always overwrites remote"),
        new(ConflictPolicy.RemoteWins,    "Remote wins",     "Remote always overwrites local"),
    ];

    [RelayCommand]
    private async Task BrowseAsync()
    {
        // Folder picker — wired via code-behind in SettingsView
        // to avoid taking a platform dependency in the ViewModel
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        account.LocalSyncPath  = LocalSyncPath;
        account.ConflictPolicy = ConflictPolicy;

        var entity = await repository.GetByIdAsync(account.Id);
        if (entity is null) return;

        entity.LocalSyncPath  = LocalSyncPath;
        entity.ConflictPolicy = ConflictPolicy;
        await repository.UpsertAsync(entity);
    }
}

public sealed partial class SettingsViewModel(
    ISettingsService   settingsService,
    IThemeService      themeService,
    SyncScheduler      scheduler,
    IAccountRepository repository) : ObservableObject
{
    // ── Appearance ────────────────────────────────────────────────────────

    [ObservableProperty] private AppTheme _theme = settingsService.Current.Theme;

    partial void OnThemeChanged(AppTheme value)
    {
        themeService.Apply(value);
        settingsService.Current.Theme = value;
        _ = settingsService.SaveAsync();
    }

    public IReadOnlyList<ThemeOption> ThemeOptions { get; } =
    [
        new(AppTheme.Light,  "Light"),
        new(AppTheme.Dark,   "Dark"),
        new(AppTheme.System, "System"),
    ];

    // ── Sync policy ───────────────────────────────────────────────────────

    [ObservableProperty]
    private ConflictPolicy _defaultConflictPolicy =
        settingsService.Current.DefaultConflictPolicy;

    partial void OnDefaultConflictPolicyChanged(ConflictPolicy value)
    {
        settingsService.Current.DefaultConflictPolicy = value;
        _ = settingsService.SaveAsync();
    }

    [ObservableProperty] private int _syncIntervalMinutes =
        settingsService.Current.SyncIntervalMinutes;

    partial void OnSyncIntervalMinutesChanged(int value)
    {
        settingsService.Current.SyncIntervalMinutes = value;
        scheduler.SetInterval(TimeSpan.FromMinutes(value));
        _ = settingsService.SaveAsync();
    }

    public IReadOnlyList<SyncIntervalOption> IntervalOptions { get; } =
    [
        new(5,   "5 minutes"),
        new(15,  "15 minutes"),
        new(30,  "30 minutes"),
        new(60,  "60 minutes"),
        new(120, "2 hours"),
    ];

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new(ConflictPolicy.Ignore,        "Ignore",          "Skip — leave both unchanged"),
        new(ConflictPolicy.KeepBoth,      "Keep both",       "Rename local, keep remote"),
        new(ConflictPolicy.LastWriteWins, "Last write wins", "Most recently modified wins"),
        new(ConflictPolicy.LocalWins,     "Local wins",      "Local always overwrites remote"),
        new(ConflictPolicy.RemoteWins,    "Remote wins",     "Remote always overwrites local"),
    ];

    // ── Per-account settings ──────────────────────────────────────────────

    public ObservableCollection<AccountSyncSettingsViewModel> AccountSettings { get; } = [];

    public void LoadAccounts(IEnumerable<OneDriveAccount> accounts)
    {
        AccountSettings.Clear();
        foreach (var a in accounts)
            AccountSettings.Add(new AccountSyncSettingsViewModel(a, repository));
    }

    public void AddAccount(OneDriveAccount account) =>
        AccountSettings.Add(new AccountSyncSettingsViewModel(account, repository));

    public void RemoveAccount(string accountId)
    {
        var vm = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if (vm is not null) AccountSettings.Remove(vm);
    }
}

public sealed record ThemeOption(AppTheme Theme, string Label);
public sealed record SyncIntervalOption(int Minutes, string Label);
