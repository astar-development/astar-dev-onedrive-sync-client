using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        _ = services.AddDbContext<OneDriveSyncDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("OneDriveSync"),
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "onedrive")));

        using IServiceScope scope = services.BuildServiceProvider().CreateScope();
        OneDriveSyncDbContext dbContext = scope.ServiceProvider.GetRequiredService<OneDriveSyncDbContext>();
        dbContext.Database.Migrate();

        return services;
    }
}
