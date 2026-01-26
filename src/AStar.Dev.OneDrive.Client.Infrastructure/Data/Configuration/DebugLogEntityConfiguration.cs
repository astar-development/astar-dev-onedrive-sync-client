using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configuration;

public sealed class DebugLogEntityConfiguration : IEntityTypeConfiguration<DebugLogEntity>
{
    public void Configure(EntityTypeBuilder<DebugLogEntity> builder)
    {
        _ = builder.ToTable("DebugLogs");
        _ = builder.HasKey(d => d.Id);

        _ = builder.HasIndex(e => e.AccountId);
        _ = builder.HasIndex(e => e.TimestampUtc);

        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
