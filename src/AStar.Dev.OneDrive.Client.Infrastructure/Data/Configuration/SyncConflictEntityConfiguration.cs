using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configuration;

public sealed class SyncConflictEntityConfiguration : IEntityTypeConfiguration<SyncConflictEntity>
{
    public void Configure(EntityTypeBuilder<SyncConflictEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Id).IsRequired();
        _ = builder.Property(e => e.AccountId).IsRequired();
        _ = builder.Property(e => e.FilePath).IsRequired();

        _ = builder.HasIndex(e => e.AccountId);
        _ = builder.HasIndex(e => new { e.AccountId, e.IsResolved });

        // Foreign key relationship with cascade delete
        _ = builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
