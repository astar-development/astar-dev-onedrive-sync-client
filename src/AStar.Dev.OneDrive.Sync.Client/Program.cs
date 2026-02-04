using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AStar.Dev.OneDrive.Sync.Client;

internal class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        IConfiguration configuration = ConfigurationFactory.Build(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting AStar OneDrive Sync Client");

            IHost host = CreateHostBuilder(args, configuration).Build();

            MigrateDatabase(host);

            host.Run();

            Log.Information("Application shut down successfully");
            return 0;
        }
        catch(Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void MigrateDatabase(IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        OneDriveSyncDbContext dbContext = scope.ServiceProvider.GetRequiredService<OneDriveSyncDbContext>();
        dbContext.Database.Migrate();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration)
        => Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                _ = config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                // Register additional services when they do not fit into AppModule
            });
}
