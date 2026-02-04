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
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .IsRequired();
        
        builder.Property(e => e.AccountId)
            .IsRequired();
        
        builder.Property(e => e.DriveItemId)
            .IsRequired();
        
        builder.Property(e => e.Name)
            .IsRequired();
        
        builder.Property(e => e.Path)
            .IsRequired();
        
        builder.Property(e => e.IsFolder)
            .IsRequired();
        
        builder.Property(e => e.IsSelected)
            .HasDefaultValue(false);
        
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}