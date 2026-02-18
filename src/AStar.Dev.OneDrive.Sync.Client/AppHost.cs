using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class AppHost
{
    public static IHost BuildHost()
    {
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationMetadata.ApplicationFolder, "logs");
        _ = Directory.CreateDirectory(logDir);

        IHost host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                IConfigurationRoot configuration = services.AddApplicationConfiguration();

                ConfigureLogging(logDir, configuration);
                _ = services.AddDatabaseServices()
                            .AddAuthenticationServices(configuration)
                            .AddApplicationServices()
                            .AddViewModels()
                            .AddAnnotatedServices()
                            .AddHostedService<LogCleanupBackgroundService>();

                services.AddHttpClientWithRetry();
            })
            .Build();

        return host;
    }
    /// <summary>
    ///     Ensures the database is created and migrations are applied.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void EnsureDatabaseUpdated(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        SyncDbContext context = scope.ServiceProvider.GetRequiredService<SyncDbContext>();

        context.Database.Migrate();
    }

    private static void ConfigureLogging(string logDir, IConfiguration configuration)
        => Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(
                    formatter: new Serilog.Formatting.Json.JsonFormatter(),
                    path: Path.Combine(logDir, "sync.txt"),
                    rollingInterval: RollingInterval.Hour,
                    retainedFileCountLimit: 7,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                )
                .CreateLogger();
}
