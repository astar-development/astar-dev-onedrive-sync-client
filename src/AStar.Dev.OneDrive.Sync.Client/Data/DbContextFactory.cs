using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

/// <summary>
/// Resolves the SQLite database path at runtime using the same
/// platform-appropriate directory as the token cache.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create()
    {
        var dir = DataDirectoryPathGenerator.GetPlatformDataDirectory();
        _ = Directory.CreateDirectory(dir);

        var dbPath = Path.Combine(dir, ApplicationMetadata.DatabaseFileName);

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }
}
