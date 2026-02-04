using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the ConflictLog entity.
/// </summary>
public class ConflictLogConfiguration : IEntityTypeConfiguration<ConflictLog>
{
    /// <summary>
    /// Configures the ConflictLog entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ConflictLog> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .IsRequired();
        
        builder.Property(e => e.AccountId)
            .IsRequired();
        
        builder.Property(e => e.ItemId)
            .IsRequired();
        
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}