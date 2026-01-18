using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStarOneDriveClient.Data;

/// <summary>
///     Design-time factory for creating SyncDbContext instances during migrations.
/// </summary>
public sealed class SyncDbContextFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    public SyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SyncDbContext>();

        // Use a temporary database path for design-time operations
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(appDataPath, "AStarOneDriveClient", "sync.db");

        _ = optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new SyncDbContext(optionsBuilder.Options);
    }
}
