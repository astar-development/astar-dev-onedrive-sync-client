using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public sealed class SyncConflictEntityConfiguration : IEntityTypeConfiguration<SyncConflictEntity>
{
    public void Configure(EntityTypeBuilder<SyncConflictEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Id).IsRequired();
        _ = builder.Property(e => e.HashedAccountId).IsRequired();
        _ = builder.Property(e => e.FilePath).IsRequired();

        _ = builder.HasIndex(e => e.HashedAccountId);
        _ = builder.HasIndex(e => new { e.HashedAccountId, e.IsResolved });

        _ = builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.HashedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Property(e => e.HashedAccountId).IsRequired()
            .HasConversion(SqliteTypeConverters.HashedAccountIdToString)
            .HasColumnType("TEXT");
    }
}
