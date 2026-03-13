using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public class FolderHierarchyConfiguration : IEntityTypeConfiguration<FolderHierarchy>
{
    public void Configure(EntityTypeBuilder<FolderHierarchy> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Name).IsRequired();
        _ = builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.FullPath)
      .ValueGeneratedOnAddOrUpdate()
      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        _ = builder.HasOne(e => e.Parent)
              .WithMany(e => e.Children)
              .HasForeignKey(e => e.ParentId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
