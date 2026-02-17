using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public sealed class DebugLogEntityConfiguration : IEntityTypeConfiguration<DebugLogEntity>
{
    public void Configure(EntityTypeBuilder<DebugLogEntity> builder)
    {
        _ = builder.ToTable("DebugLogs");
        _ = builder.HasKey(d => d.Id);

        _ = builder.HasIndex(e => e.TimestampUtc);

        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.HashedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Property(e => e.HashedAccountId).IsRequired()
            .HasConversion(SqliteTypeConverters.HashedAccountIdToString)
            .HasColumnType("TEXT");
    }
}
