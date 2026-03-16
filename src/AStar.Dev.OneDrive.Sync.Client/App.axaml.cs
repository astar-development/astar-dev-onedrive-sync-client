using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using AStar.Dev.OneDrive.Sync.Client.Views;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;
using AStar.Dev.OneDrive.Sync.Client.Services;
using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class App : Application
{
    // ── Service singletons (replaced with a proper DI container in a later step) ──
    public static ILocalizationService Localisation { get; private set; } = null!;
    public static IThemeService        Theme        { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        // ── Localisation ─────────────────────────────────────────────────
        var locService = new LocalizationService();
        await locService.InitialiseAsync(new CultureInfo("en-GB"));
        Localisation = locService;

        // ── Theme ────────────────────────────────────────────────────────
        var themeService = new ThemeService();
        themeService.Apply(AppTheme.System); // System default; user can change in Settings
        Theme = themeService;

        // ── Main window ──────────────────────────────────────────────────
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}