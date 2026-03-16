using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class App : Application
{
    // ── Service singletons ────────────────────────────────────────────────
    public static ILocalizationService Localisation { get; private set; } = null!;
    public static IThemeService        Theme        { get; private set; } = null!;
    public static IAuthService         Auth         { get; private set; } = null!;
    public static IAccountRepository   Repository   { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        // ── Localisation ─────────────────────────────────────────────────
        var locService = new LocalizationService();
        await locService.InitialiseAsync(new CultureInfo("en-GB"));
        Localisation = locService;

        // ── Theme ────────────────────────────────────────────────────────
        var themeService = new ThemeService();
        themeService.Apply(AppTheme.System);
        Theme = themeService;

        // ── Database ─────────────────────────────────────────────────────
        var db = DbContextFactory.Create();
        await db.Database.MigrateAsync();
        var repository = new AccountRepository(db);
        Repository = repository;

        // ── Auth ─────────────────────────────────────────────────────────
        var tokenCache  = new TokenCacheService();
        var authService = new AuthService(tokenCache);
        Auth = authService;

        // ── Graph ─────────────────────────────────────────────────────────
        var graphService = new GraphService();

        // ── Startup service ───────────────────────────────────────────────
        var startupService = new StartupService(repository, authService);

        // ── Main window ──────────────────────────────────────────────────
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(authService, graphService, startupService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
