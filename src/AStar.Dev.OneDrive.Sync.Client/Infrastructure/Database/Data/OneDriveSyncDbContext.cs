using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;

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
    /// Gets or sets the Accounts DbSet.
    /// </summary>
    public DbSet<Account> Accounts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DeltaTokens DbSet.
    /// </summary>
    public DbSet<DeltaToken> DeltaTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the FileSystemItems DbSet.
    /// </summary>
    public DbSet<FileSystemItem> FileSystemItems { get; set; } = null!;

    /// <summary>
    /// Configures the model for the context, including schema configuration and entity configurations.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities to use the 'onedrive' schema
        modelBuilder.HasDefaultSchema("onedrive");

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new DeltaTokenConfiguration());
        modelBuilder.ApplyConfiguration(new FileSystemItemConfiguration());
    }
}