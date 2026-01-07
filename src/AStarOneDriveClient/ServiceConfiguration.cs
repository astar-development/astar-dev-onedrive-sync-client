using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Data;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Services.OneDriveServices;
using AStarOneDriveClient.Services.Sync;
using AStarOneDriveClient.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace AStarOneDriveClient;

/// <summary>
/// Configures dependency injection services for the application.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures and returns the service provider with all application services.
    /// </summary>
    /// <returns>Configured service provider.</returns>
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Database
        services.AddDbContext<SyncDbContext>(options =>
            options.UseSqlite(DatabaseConfiguration.ConnectionString));

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
        services.AddScoped<ISyncStateRepository, SyncStateRepository>();
        services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        services.AddScoped<ISyncConflictRepository, SyncConflictRepository>();
        services.AddScoped<ISyncSessionLogRepository, SyncSessionLogRepository>();
        services.AddScoped<IFileOperationLogRepository, FileOperationLogRepository>();

        // Load authentication configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var authConfig = AuthConfiguration.LoadFromConfiguration(configuration);

        // Authentication - registered as singleton with factory
        services.AddSingleton<IAuthService>(provider =>
        {
            // AuthService.CreateAsync must be called synchronously during startup
            // This is acceptable as it's a one-time initialization cost
            return AuthService.CreateAsync(authConfig).GetAwaiter().GetResult();
        });

        // Services
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IFileWatcherService, FileWatcherService>();
        services.AddSingleton<IAutoSyncCoordinator, AutoSyncCoordinator>();
        services.AddScoped<IWindowPreferencesService, WindowPreferencesService>();
        services.AddScoped<IGraphApiClient, GraphApiClient>();
        services.AddScoped<IFolderTreeService, FolderTreeService>();
        services.AddScoped<ISyncSelectionService, SyncSelectionService>();
        services.AddScoped<ILocalFileScanner, LocalFileScanner>();
        services.AddScoped<IRemoteChangeDetector, RemoteChangeDetector>();
        services.AddScoped<IConflictResolver, ConflictResolver>();
        services.AddScoped<ISyncEngine, SyncEngine>();

        // ViewModels
        services.AddTransient<AccountManagementViewModel>();
        services.AddTransient<SyncTreeViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ConflictResolutionViewModel>();
        services.AddTransient<SyncProgressViewModel>();
        services.AddTransient<UpdateAccountDetailsViewModel>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void EnsureDatabaseCreated(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
        context.Database.EnsureCreated();
    }
}
