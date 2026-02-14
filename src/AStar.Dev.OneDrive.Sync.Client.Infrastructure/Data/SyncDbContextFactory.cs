using AStar.Dev.OneDrive.Sync.Client.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;

/// <summary>
///     Design-time factory for creating SyncDbContext instances during migrations.
/// </summary>
public sealed class SyncDbContextFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    public SyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SyncDbContext>();

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(appDataPath, ApplicationMetadata.ApplicationFolder, "sync.db");

        _ = optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new SyncDbContext(optionsBuilder.Options);
    }
}
