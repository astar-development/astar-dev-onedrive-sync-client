using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AStar.Dev.OneDrive.Sync.Client.SyncronisationConflicts;
using AStar.Dev.Source.Generators.OptionsBindingGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class ApplicationServiceExtensions
{
    /// <summary>
    ///     Adds application-specific services to the DI container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();
        _ = services.AddSingleton<IFileWatcherService, FileWatcherService>();
        _ = services.AddSingleton<IAutoSyncCoordinator, AutoSyncCoordinator>();
        _ = services.AddSingleton<IAutoSyncSchedulerService, AutoSyncSchedulerService>();
        _ = services.AddScoped<IWindowPreferencesService, WindowPreferencesService>();
        _ = services.AddScoped<IGraphApiClient, GraphApiClient>();
        _ = services.AddScoped<IFolderTreeService, FolderTreeService>();
        _ = services.AddScoped<ISyncSelectionService, SyncSelectionService>();
        _ = services.AddScoped<ILocalFileScanner, LocalFileScanner>();
        _ = services.AddScoped<IRemoteChangeDetector, RemoteChangeDetector>();
        _ = services.AddScoped<IConflictResolver, ConflictResolver>();
        _ = services.AddScoped<IConflictDetectionService, ConflictDetectionService>();
        _ = services.AddScoped<IDeletionSyncService, DeletionSyncService>();
        _ = services.AddScoped<ISyncEngine, SyncEngine>();
        _ = services.AddScoped<IFileTransferService, FileTransferService>();
        _ = services.AddScoped<IDebugLogger, DebugLoggerService>();
        _ = services.AddScoped<IDeltaPageProcessor, DeltaPageProcessor>();
        _ = services.AddScoped<IDeltaProcessingService, DeltaProcessingService>();
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();
        _ = services.AddScoped<ISyncStateCoordinator, SyncStateCoordinator>();
        _ = services.AddScoped<IThemeService, ThemeService>();
        _ = services.AddScoped<IThemeStartupCoordinator, ThemeStartupCoordinator>();
        _ = services.AddScoped<SettingsViewModel>();

        return services;
    }
    public static IConfigurationRoot AddApplicationConfiguration(this IServiceCollection services)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .Build();
        _ = services.AddAutoRegisteredOptions(configuration);

        return configuration;
    }
}
