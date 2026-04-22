using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOneDriveSyncServices(this IServiceCollection services, ISettingsService settings)
    {
        _ = services.AddSingleton(settings);
        _ = services.AddSingleton<ILocalizationService, LocalizationService>();
        _ = services.AddSingleton<IThemeService, ThemeService>();
        _ = services.AddSingleton<TokenCacheService>();
        _ = services.AddSingleton<IAuthService, AuthService>();
        _ = services.AddSingleton<IGraphService, GraphService>();
        _ = services.AddSingleton(_ => DbContextFactory.Create());
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddSingleton<ISyncRepository, SyncRepository>();
        _ = services.AddSingleton<ISyncService, SyncService>();
        _ = services.AddSingleton<IStartupService, StartupService>();
        _ = services.AddSingleton<SyncScheduler>();
        _ = services.AddTransient<MainWindowViewModel>();

        return services;
    }
}
