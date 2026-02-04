using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.DbContext;

/// <summary>
/// Database context for OneDrive sync application using PostgreSQL with the 'onedrive' schema.
/// </summary>
public class OneDriveSyncDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveSyncDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public OneDriveSyncDbContext(DbContextOptions<OneDriveSyncDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the model for the context, including schema configuration.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities to use the 'onedrive' schema
        modelBuilder.HasDefaultSchema("onedrive");
    }
}