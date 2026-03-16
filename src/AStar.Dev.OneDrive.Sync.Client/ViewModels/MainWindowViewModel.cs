using AStar.Dev.OneDrive.Sync.Client;  // for NavSection
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

/// <summary>
/// Shell-level view model.  Owns:
///   • Which <see cref="NavSection"/> is active in the icon rail.
///   • The <see cref="StatusBarViewModel"/> for the bottom bar.
///   • Placeholder properties for the account panel (populated once
///     AccountService is wired up in a later step).
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    // ── Navigation ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    private NavSection _activeSection = NavSection.Dashboard;

    public bool IsDashboardActive => ActiveSection == NavSection.Dashboard;
    public bool IsFilesActive      => ActiveSection == NavSection.Files;
    public bool IsActivityActive   => ActiveSection == NavSection.Activity;
    public bool IsAccountsActive   => ActiveSection == NavSection.Accounts;
    public bool IsSettingsActive   => ActiveSection == NavSection.Settings;

    [RelayCommand]
    private void Navigate(NavSection section) => ActiveSection = section;

    // ── Status bar ────────────────────────────────────────────────────────

    public StatusBarViewModel StatusBar { get; } = new();

    // ── Account panel (stub — replaced when AccountService is injected) ───

    /// <summary>
    /// Display name of the currently-focused account, shown above the status bar.
    /// Empty when no account is selected.
    /// </summary>
    [ObservableProperty] private string _activeAccountName = string.Empty;
}
