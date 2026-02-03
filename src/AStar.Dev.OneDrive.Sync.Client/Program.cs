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
        var configuration = BuildConfiguration(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting AStar OneDrive Sync Client");

            var host = CreateHostBuilder(args, configuration).Build();
            
            // TODO: Apply pending EF Core migrations on startup (Phase 1)
            
            host.Run();

            Log.Information("Application shut down successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IConfiguration BuildConfiguration(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                         ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                         ?? "Production";

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Replace default configuration with our pre-built configuration
                config.Sources.Clear();
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                // TODO: Register application services (Phase 1)
                // services.AddDbContext<OneDriveSyncDbContext>();
                // services.AddSingleton<ISecureTokenStorage, ...>();
                // services.AddScoped<IAuthenticationService, ...>();
            });
}
