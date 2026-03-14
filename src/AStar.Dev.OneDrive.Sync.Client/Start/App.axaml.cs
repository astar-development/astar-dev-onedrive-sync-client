using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.OneDrive.Sync.Client.Start;

/// <summary>
///     Main application class for the OneDrive sync client.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class App : Application
{
    public static IHost Host { get; private set; } = null!;

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
        => _ = Try.Run(StartTheApplication)
                  .Tap(AttemptToInformTheUserOfTheFailure);

    private void StartTheApplication()
    {
        Host = AppHost.BuildHost();
        IServiceProvider services = Host.Services;

        _ = Host.StartAsync();
        AppHost.EnsureDatabaseUpdated(services);

        IThemeStartupCoordinator themeCoordinator = services.GetRequiredService<IThemeStartupCoordinator>();
        themeCoordinator.InitializeThemeOnStartupAsync(CancellationToken.None).GetAwaiter().GetResult();

        IAutoSyncSchedulerService scheduler = services.GetRequiredService<IAutoSyncSchedulerService>();
        _ = scheduler.StartAsync();

        IDebugLogRepository debugLogger = services.GetRequiredService<IDebugLogRepository>();
        IAccountRepository accountRepository = services.GetRequiredService<IAccountRepository>();
        DebugLog.Initialize(debugLogger, accountRepository);

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Home.MainWindow(Host);

            desktop.Startup += async (_, _) => await DebugLog.LogInfoAsync("App Startup", new Core.Models.HashedAccountId(AdminAccountMetadata.HashedAccountId), "Application has started", CancellationToken.None);

            desktop.Exit += (_, _) => scheduler.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void AttemptToInformTheUserOfTheFailure(bool ex)
    {
        // If initialization fails, log the error and show a message box to the user.
        // Since we may not have a logger available, we'll write to the console as a fallback.
        Console.Error.WriteLine($"Fatal error during application startup: {ex}");

        // Attempt to show a message box to the user. This may fail if the UI framework isn't initialized, so we catch any exceptions.
        try
        {
            var messageBox = new Window
            {
                Title = "Startup Error",
                Content = new TextBlock
                {
                    Text = "An unexpected error occurred during application startup. Please check the logs for more details.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20)
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            messageBox.Show();
        }
        catch
        {
            // If we can't show a message box, there's not much else we can do at this point.
        }
    }
}
