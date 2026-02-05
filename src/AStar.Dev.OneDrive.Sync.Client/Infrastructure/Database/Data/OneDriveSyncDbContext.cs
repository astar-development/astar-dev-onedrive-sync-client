using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;

/// <summary>
/// Database context for OneDrive sync application using PostgreSQL with the 'onedrive' schema.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OneDriveSyncDbContext"/> class.
/// </remarks>
/// <param name="options">The options to configure the context.</param>
public class OneDriveSyncDbContext(DbContextOptions<OneDriveSyncDbContext> options) : DbContext(options)
{

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
    /// Gets or sets the ConflictLogs DbSet.
    /// </summary>
    public DbSet<ConflictLog> ConflictLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SyncHistory DbSet.
    /// </summary>
    public DbSet<SyncHistory> SyncHistory { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DiagnosticSettings DbSet.
    /// </summary>
    public DbSet<DiagnosticSettings> DiagnosticSettings { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ApplicationLogs DbSet.
    /// </summary>
    public DbSet<ApplicationLog> ApplicationLogs { get; set; } = null!;

    /// <summary>
    /// Configures the model for the context, including schema configuration and entity configurations.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(OneDriveSyncDbContext).Assembly);
    }
}
