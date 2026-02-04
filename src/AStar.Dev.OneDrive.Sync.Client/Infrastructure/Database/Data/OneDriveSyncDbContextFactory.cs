using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;

/// <summary>
/// Factory for creating OneDriveSyncDbContext instances at design time.
/// Used by EF Core tools (migrations, etc.).
/// </summary>
public class OneDriveSyncDbContextFactory : IDesignTimeDbContextFactory<OneDriveSyncDbContext>
{
    /// <summary>
    /// Creates a new instance of OneDriveSyncDbContext.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A configured OneDriveSyncDbContext instance.</returns>
    public OneDriveSyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OneDriveSyncDbContext>();

        _ = optionsBuilder.UseNpgsql(
            CreateDesignTimeConnectionString(),
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "onedrive"));

        return new OneDriveSyncDbContext(optionsBuilder.Options);

        static string CreateDesignTimeConnectionString()
        {
            return "Host=localhost;Port=5432;Database=astar-dev-onedrive-sync-db;Username=astar-admin;Password=placeholder;Schema=onedrive";
        }
    }
}
