using Avalonia;
using ReactiveUI.Avalonia;
using Serilog;
using static AStar.Dev.Logging.Extensions.Serilog.SerilogExtensions;

namespace AStar.Dev.OneDrive.Client;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = CreateMinimalLogger();

        try
        {
            _ = BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
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
            .UseReactiveUI();
}
