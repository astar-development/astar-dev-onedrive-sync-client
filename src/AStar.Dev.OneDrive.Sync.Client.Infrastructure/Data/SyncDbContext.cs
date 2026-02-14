using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;

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
    public DbSet<DriveItemEntity> DriveItems { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(SyncDbContext).Assembly);

        modelBuilder.UseSqliteFriendlyConversions();
    }
}
