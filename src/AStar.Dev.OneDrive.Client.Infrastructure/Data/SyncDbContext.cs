using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data;

/// <summary>
///     Database context for the OneDrive sync application.
/// </summary>
public sealed class SyncDbContext(DbContextOptions<SyncDbContext> options) : DbContext(options)
{
    /// <summary>
    ///     Gets or sets the accounts.
    /// </summary>
    public DbSet<AccountEntity> Accounts { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the sync configurations.
    /// </summary>
    public DbSet<SyncConfigurationEntity> SyncConfigurations { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the file metadata.
    /// </summary>
    public DbSet<FileMetadataEntity> FileMetadata { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the window preferences.
    /// </summary>
    public DbSet<WindowPreferencesEntity> WindowPreferences { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the sync conflicts.
    /// </summary>
    public DbSet<SyncConflictEntity> SyncConflicts { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the sync session logs.
    /// </summary>
    public DbSet<SyncSessionLogEntity> SyncSessionLogs { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the file operation logs.
    /// </summary>
    public DbSet<FileOperationLogEntity> FileOperationLogs { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the debug logs.
    /// </summary>
    public DbSet<DebugLogEntity> DebugLogs { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the Delta Tokens.
    /// </summary>
    public DbSet<DeltaToken> DeltaTokens { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the Delta Tokens.
    /// </summary>
    public DbSet<DriveItemRecord> DriveItems { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            _ = optionsBuilder.UseSqlite(@"Data Source=C:\Users\jbarden\AppData\Local\AStar.Dev.OneDrive.Client\sync.db");
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(SyncDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }
}
