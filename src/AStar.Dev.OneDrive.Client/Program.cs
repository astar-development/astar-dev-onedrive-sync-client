using Avalonia;
using ReactiveUI.Avalonia;
using Serilog;

namespace AStar.Dev.OneDrive.Client;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            _ = BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch(Exception ex)
        {
            Log.Fatal(ex, $"{ApplicationMetadata.ApplicationName} terminated unexpectedly");
        }
        finally
        {
            Log.Information($"{ApplicationMetadata.ApplicationName} Shutting Down");
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
