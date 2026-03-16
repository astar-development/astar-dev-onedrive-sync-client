using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity>      Accounts      => Set<AccountEntity>();
    public DbSet<SyncFolderEntity>   SyncFolders   => Set<SyncFolderEntity>();
    public DbSet<SyncConflictEntity> SyncConflicts => Set<SyncConflictEntity>();
    public DbSet<SyncJobEntity>      SyncJobs      => Set<SyncJobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasMany(a => a.SyncFolders)
             .WithOne(f => f.Account)
             .HasForeignKey(f => f.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany<SyncConflictEntity>()
             .WithOne(c => c.Account)
             .HasForeignKey(c => c.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany<SyncJobEntity>()
             .WithOne(j => j.Account)
             .HasForeignKey(j => j.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SyncFolderEntity>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.AccountId, f.FolderId }).IsUnique();
        });

        modelBuilder.Entity<SyncConflictEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.AccountId, c.State });
        });

        modelBuilder.Entity<SyncJobEntity>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => new { j.AccountId, j.State });
        });
    }
}
