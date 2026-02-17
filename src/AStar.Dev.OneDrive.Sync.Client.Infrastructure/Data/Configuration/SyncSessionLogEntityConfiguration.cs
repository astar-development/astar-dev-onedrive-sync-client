using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public sealed class SyncSessionLogEntityConfiguration : IEntityTypeConfiguration<SyncSessionLogEntity>
{
    public void Configure(EntityTypeBuilder<SyncSessionLogEntity> builder)
    {
        _ = builder.ToTable("SyncSessionLogs");
        _ = builder.HasKey(e => e.Id);
        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.HashedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.Property(e => e.HashedAccountId).IsRequired()
            .HasConversion(SqliteTypeConverters.HashedAccountIdToString)
            .HasColumnType("TEXT");
    }
}

