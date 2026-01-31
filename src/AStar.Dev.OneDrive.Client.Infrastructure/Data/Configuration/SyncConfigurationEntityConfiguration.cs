using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configuration;

public sealed class SyncConfigurationEntityConfiguration : IEntityTypeConfiguration<SyncConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<SyncConfigurationEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId).IsRequired();
        _ = builder.Property(e => e.FolderPath).IsRequired();
        _ = builder.HasIndex(e => new { e.AccountId, e.FolderPath });

        // Foreign key relationship with cascade delete
        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
