using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configuration;

public sealed class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadataEntity>
{
    public void Configure(EntityTypeBuilder<FileMetadataEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Id).IsRequired();
        _ = builder.Property(e => e.AccountId).IsRequired();
        _ = builder.Property(e => e.Name).IsRequired();
        _ = builder.Property(e => e.Path).IsRequired();
        _ = builder.Property(e => e.LocalPath).IsRequired();

        _ = builder.HasIndex(e => e.AccountId);
        _ = builder.HasIndex(e => new { e.AccountId, e.Path });

        // Foreign key relationship with cascade delete
        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
