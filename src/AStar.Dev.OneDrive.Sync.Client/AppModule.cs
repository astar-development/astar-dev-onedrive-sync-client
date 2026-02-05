using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class AppModule
{
    public static IServiceCollection AddAppModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.AddConfiguration(configuration);
        _ = services.AddInfrastructureServices(configuration);

        return services;
    }

    private static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<AuthenticationOptions>(
            configuration.GetSection(AuthenticationOptions.SectionName));

        _ = services.Configure<SyncOptions>(
            configuration.GetSection(SyncOptions.SectionName));

        _ = services.Configure<TelemetryOptions>(
            configuration.GetSection(TelemetryOptions.SectionName));

        _ = services.AddSingleton(sp =>
            configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new());

        _ = services.AddSingleton(sp =>
            configuration.GetSection(SyncOptions.SectionName).Get<SyncOptions>() ?? new());

        _ = services.AddSingleton(sp =>
            configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new());

        return services;
    }

    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddSingleton<ResiliencePolicyFactory>();

        _ = services.AddSingleton<SecureTokenStorageFactory>();
        _ = services.AddSingleton(sp =>
        {
            SecureTokenStorageFactory factory = sp.GetRequiredService<SecureTokenStorageFactory>();

            return factory.CreateStorage();
        });
        
        // Register logging
        _ = services.AddLogging();
        
        // Register MSAL PublicClientApplication
        _ = services.AddSingleton<IPublicClientApplication>(sp =>
        {
            var authOptions = sp.GetRequiredService<AuthenticationOptions>();
            return PublicClientApplicationBuilder
                .Create(authOptions.Microsoft.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, authOptions.Microsoft.TenantId)
                .WithRedirectUri(authOptions.Microsoft.RedirectUri)
                .Build();
        });
        
        // Register Graph API client (mock for now, Phase 3 will replace with Kiota client)
        _ = services.AddScoped<Infrastructure.GraphApi.IGraphApiClient, Infrastructure.GraphApi.MockGraphApiClient>();
        
        // Build OS-specific database path in user's AppData folder
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbDirectory = Path.Combine(appDataPath, "AStar.Dev.OneDrive.Sync.Client");
        Directory.CreateDirectory(dbDirectory); // Ensure directory exists
        
        string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        string dbFileName = environment == "Development" ? "onedrive-sync-dev.db" : "onedrive-sync.db";
        string dbPath = Path.Combine(dbDirectory, dbFileName);
        string connectionString = $"Data Source={dbPath}";
        
        _ = services.AddDbContext<OneDriveSyncDbContext>(options => options.UseSqlite(connectionString));

        using IServiceScope scope = services.BuildServiceProvider().CreateScope();
        OneDriveSyncDbContext dbContext = scope.ServiceProvider.GetRequiredService<OneDriveSyncDbContext>();
        dbContext.Database.Migrate();

        return services;
    }
}
