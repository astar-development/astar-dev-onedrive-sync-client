using AStar.Dev.OneDrive.Client.Core;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client;

/// <summary>
///     Main application class for the OneDrive sync client.
/// </summary>
public sealed class App : Application
{
    /// <summary>
    ///     Gets the service provider for dependency injection.
    /// </summary>
    public static ServiceProvider? Services { get; private set; }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Configure dependency injection
        Services = ServiceConfiguration.ConfigureServices();
        ServiceConfiguration.EnsureDatabaseUpdated(Services);

        // Initialize static debug logger
        IDebugLogger debugLogger = Services.GetRequiredService<IDebugLogger>();
        DebugLog.Initialize(debugLogger);

        // Start auto-sync scheduler
        IAutoSyncSchedulerService scheduler = Services.GetRequiredService<IAutoSyncSchedulerService>();
        _ = scheduler.StartAsync();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow.MainWindow();

            desktop.Startup += async (_, _) => await DebugLog.InfoAsync("App Startup", "Application has started", AdminAccountMetadata.AccountId, CancellationToken.None);

            // Cleanup on exit
            desktop.Exit += (_, _) =>
            {
                scheduler.Dispose();
                Services?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
