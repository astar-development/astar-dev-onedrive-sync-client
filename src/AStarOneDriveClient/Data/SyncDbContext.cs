using AStarOneDriveClient.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Data;

/// <summary>
/// Database context for the OneDrive sync application.
/// </summary>
public sealed class SyncDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the accounts table.
    /// </summary>
    public DbSet<AccountEntity> Accounts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sync configurations table.
    /// </summary>
    public DbSet<SyncConfigurationEntity> SyncConfigurations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sync states table.
    /// </summary>
    public DbSet<SyncStateEntity> SyncStates { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file metadata table.
    /// </summary>
    public DbSet<FileMetadataEntity> FileMetadata { get; set; } = null!;

    /// <summary>
    /// Gets or sets the window preferences table.
    /// </summary>
    public DbSet<WindowPreferencesEntity> WindowPreferences { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sync conflicts table.
    /// </summary>
    public DbSet<SyncConflictEntity> SyncConflicts { get; set; } = null!;

    public SyncDbContext(DbContextOptions<SyncDbContext> options) : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure AccountEntity
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired();
            entity.Property(e => e.LocalSyncPath).IsRequired();
            entity.HasIndex(e => e.LocalSyncPath).IsUnique();
        });

        // Configure SyncConfigurationEntity
        modelBuilder.Entity<SyncConfigurationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.FolderPath).IsRequired();
            entity.HasIndex(e => new { e.AccountId, e.FolderPath });

            // Foreign key relationship with cascade delete
            entity.HasOne<AccountEntity>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SyncStateEntity
        modelBuilder.Entity<SyncStateEntity>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).IsRequired();

            // Foreign key relationship with cascade delete
            entity.HasOne<AccountEntity>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FileMetadataEntity
        modelBuilder.Entity<FileMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.LocalPath).IsRequired();

            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => new { e.AccountId, e.Path });

            // Foreign key relationship with cascade delete
            entity.HasOne<AccountEntity>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WindowPreferencesEntity
        modelBuilder.Entity<WindowPreferencesEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Width).HasDefaultValue(800);
            entity.Property(e => e.Height).HasDefaultValue(600);
        });

        // Configure SyncConflictEntity
        modelBuilder.Entity<SyncConflictEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.AccountId).IsRequired();
            entity.Property(e => e.FilePath).IsRequired();

            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => new { e.AccountId, e.IsResolved });

            // Foreign key relationship with cascade delete
            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
