using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AStarOneDriveClient;

/// <summary>
/// Main application class for the OneDrive sync client.
/// </summary>
public sealed class App : Application
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public static ServiceProvider? Services { get; private set; }

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        // Configure dependency injection
        Services = ServiceConfiguration.ConfigureServices();
        ServiceConfiguration.EnsureDatabaseCreated(Services);

        // Initialize static debug logger
        var debugLogger = Services.GetRequiredService<IDebugLogger>();
        DebugLog.Initialize(debugLogger);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            // Cleanup on exit
            desktop.Exit += (_, _) =>
            {
                Services?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
