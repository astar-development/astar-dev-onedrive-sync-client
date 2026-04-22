using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client;

[ExcludeFromCodeCoverage]
public partial class App : Application
{
    private const string AppName = "AStar.Dev.OneDrive.Sync";

    private static ServiceProvider? _serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public static string GetPlatformUserDataDirectory(string email)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var safeEmail = string.Concat(email.Split(Path.GetInvalidFileNameChars()));

        return OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, safeEmail)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support", AppName, safeEmail)
                : Path.Combine(home, ".config", AppName, safeEmail);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            mainWindow.Opened += async (_, _) => await BootstrapAsync(mainWindow);

            desktop.Exit += async (_, _) =>
            {
                if(_serviceProvider is not null)
                    await _serviceProvider.DisposeAsync();

                Serilog.Log.Information("[App] Application exiting");
                Serilog.Log.CloseAndFlush();
            };
        }
    }

    private static async Task BootstrapAsync(MainWindow window)
    {
        try
        {
            ISettingsService settings = await SettingsService.LoadAsync();

            var services = new ServiceCollection();
            _ = services.AddOneDriveSyncServices(settings);
            _serviceProvider = services.BuildServiceProvider();

            ILocalizationService locService = _serviceProvider.GetRequiredService<ILocalizationService>();
            await locService.InitialiseAsync(new CultureInfo("en-GB"));

            IThemeService themeService = _serviceProvider.GetRequiredService<IThemeService>();
            themeService.Apply(settings.Current.Theme);

            AppDbContext db = _serviceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            MainWindowViewModel vm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            await window.InitialiseAsync(vm);

            SyncScheduler scheduler = _serviceProvider.GetRequiredService<SyncScheduler>();
            scheduler.Start(TimeSpan.FromMinutes(settings.Current.SyncIntervalMinutes));
        }
        catch(Exception ex)
        {
            Serilog.Log.Fatal(ex, "[App] Fatal error during bootstrap: {Message}", ex.Message);
        }
    }
}
