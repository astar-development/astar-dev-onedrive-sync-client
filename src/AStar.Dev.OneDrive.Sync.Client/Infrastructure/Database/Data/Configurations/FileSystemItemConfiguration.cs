using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the FileSystemItem entity.
/// </summary>
public class FileSystemItemConfiguration : IEntityTypeConfiguration<FileSystemItem>
{
    /// <summary>
    /// Configures the FileSystemItem entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<FileSystemItem> builder)
    {
        _ = builder.HasKey(e => e.Id);

        _ = builder.Property(e => e.Id)
            .IsRequired();

        _ = builder.Property(e => e.AccountId)
            .IsRequired();

        _ = builder.Property(e => e.DriveItemId)
            .IsRequired();

        _ = builder.Property(e => e.Name)
            .IsRequired();

        _ = builder.Property(e => e.Path)
            .IsRequired();

        _ = builder.Property(e => e.IsFolder)
            .IsRequired();

        _ = builder.Property(e => e.IsSelected)
            .HasDefaultValue(false);

        _ = builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
