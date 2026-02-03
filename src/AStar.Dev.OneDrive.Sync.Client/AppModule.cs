using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class AppModule
{
    public static IServiceCollection AddAppModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddConfiguration(configuration);
        services.AddInfrastructureServices();

        return services;
    }

    private static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthenticationOptions>(
            configuration.GetSection(AuthenticationOptions.SectionName));
        
        services.Configure<SyncOptions>(
            configuration.GetSection(SyncOptions.SectionName));
        
        services.Configure<TelemetryOptions>(
            configuration.GetSection(TelemetryOptions.SectionName));

        services.AddSingleton(sp => 
            configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new());
        
        services.AddSingleton(sp => 
            configuration.GetSection(SyncOptions.SectionName).Get<SyncOptions>() ?? new());
        
        services.AddSingleton(sp => 
            configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new());

        return services;
    }

    private static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddSingleton<ResiliencePolicyFactory>();

        return services;
    }
}