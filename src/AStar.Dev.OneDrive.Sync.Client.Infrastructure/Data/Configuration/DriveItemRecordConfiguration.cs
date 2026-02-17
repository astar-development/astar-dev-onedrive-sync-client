using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public sealed class DriveItemRecordConfiguration : IEntityTypeConfiguration<DriveItemEntity>
{
    public void Configure(EntityTypeBuilder<DriveItemEntity> builder)
    {
        _ = builder.ToTable("DriveItems");
        _ = builder.HasKey(d => d.DriveItemId);

        _ = builder.Property("RelativePath").IsRequired();

        _ = builder.HasIndex(e => e.IsFolder);
        _ = builder.HasIndex(e => e.IsSelected);
        _ = builder.HasIndex(e => new { e.HashedAccountId, e.RelativePath });

        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.HashedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Property(e => e.HashedAccountId).IsRequired()
            .HasConversion(SqliteTypeConverters.HashedAccountIdToString)
            .HasColumnType("TEXT");
    }
}
