using AStar.Dev.OneDrive.Sync.Client.Core.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        _ = services.AddDbContextFactory<SyncDbContext>(options => _ = options.UseSqlite(DatabaseConfiguration.ConnectionString));
        _ = services.AddDbContext<SyncDbContext>(options => options.UseSqlite(DatabaseConfiguration.ConnectionString));
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();

        _ = services.AddScoped<IAccountRepository, AccountRepository>();
        _ = services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
        _ = services.AddScoped<IDriveItemsRepository, DriveItemsRepository>();
        _ = services.AddScoped<ISyncConflictRepository, SyncConflictRepository>();
        _ = services.AddScoped<ISyncSessionLogRepository, SyncSessionLogRepository>();
        _ = services.AddScoped<IFileOperationLogRepository, FileOperationLogRepository>();
        _ = services.AddScoped<IDebugLogRepository, DebugLogRepository>();

        return services;
    }
}
