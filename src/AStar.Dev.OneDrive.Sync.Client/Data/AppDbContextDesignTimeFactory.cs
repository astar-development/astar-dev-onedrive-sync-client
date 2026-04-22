using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

/// <summary>
/// Used only by EF Core tooling (dotnet ef migrations add).
/// Never called at runtime.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design_time_temp.db")
            .Options;

        return new AppDbContext(options);
    }
}
