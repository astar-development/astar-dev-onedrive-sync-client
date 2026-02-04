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
        
        // Use a temporary connection string for design-time only
        // The actual connection string comes from configuration at runtime
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=astar-dev-onedrive-sync-db;Username=astar-admin;Password=placeholder;Schema=onedrive",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "onedrive"));

        return new OneDriveSyncDbContext(optionsBuilder.Options);
    }
}