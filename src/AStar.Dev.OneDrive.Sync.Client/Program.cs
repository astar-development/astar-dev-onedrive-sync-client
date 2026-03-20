using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Json;

namespace AStar.Dev.OneDrive.Sync.Client;

[ExcludeFromCodeCoverage(Justification = "Framework code...largely...")]
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            var logPath = Path.Combine(
                DataDirectoryPathGenerator.GetPlatformDataDirectory(),
                "sync.txt");

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .MinimumLevel.Information()
                .WriteTo.File(
                    new JsonFormatter(),
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                )
                .CreateLogger();

            AppBuilder appBuilder = BuildAvaloniaApp();

            _ = appBuilder.StartWithClassicDesktopLifetime(args);
        }
        catch(Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new X11PlatformOptions { EnableIme = false })
            .AfterSetup(_ => AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Log.Fatal(e.ExceptionObject as Exception, "[Unhandled] {Message}", (e.ExceptionObject as Exception)?.Message ?? "Unknown");
                Log.CloseAndFlush();
            });
}
