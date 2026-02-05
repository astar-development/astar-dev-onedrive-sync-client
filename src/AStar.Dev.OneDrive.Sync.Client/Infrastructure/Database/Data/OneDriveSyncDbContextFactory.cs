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

       
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbDirectory = Path.Combine(appDataPath, "AStar.Dev.OneDrive.Sync.Client");
        Directory.CreateDirectory(dbDirectory);
        string dbPath = Path.Combine(dbDirectory, "onedrive-sync.db");
        
        _ = optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new OneDriveSyncDbContext(optionsBuilder.Options);
    }
}
