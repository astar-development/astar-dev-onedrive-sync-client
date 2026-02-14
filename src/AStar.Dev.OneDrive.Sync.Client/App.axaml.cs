using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.OneDrive.Sync.Client;

/// <summary>
///     Main application class for the OneDrive sync client.
/// </summary>
public sealed class App : Application
{
    public static IHost Host { get; private set; } = null!;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        Host = AppHost.BuildHost();
        IServiceProvider services = Host.Services;

        _ = Host.StartAsync();
        AppHost.EnsureDatabaseUpdated(services);

        IThemeStartupCoordinator themeCoordinator = services.GetRequiredService<IThemeStartupCoordinator>();
        themeCoordinator.InitializeThemeOnStartupAsync(CancellationToken.None).GetAwaiter().GetResult();

        IAutoSyncSchedulerService scheduler = services.GetRequiredService<IAutoSyncSchedulerService>();
        _ = scheduler.StartAsync();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow.MainWindow();

            desktop.Startup += async (_, _) => await DebugLog.InfoAsync("App Startup", "Application has started", AdminAccountMetadata.AccountId, CancellationToken.None);

            desktop.Exit += (_, _) => scheduler.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
