using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the Account entity.
/// </summary>
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <summary>
    /// Configures the Account entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.HashedEmail)
            .IsUnique();
        
        builder.Property(e => e.Id)
            .IsRequired();
        
        builder.Property(e => e.HashedEmail)
            .IsRequired();
        
        builder.Property(e => e.MaxConcurrentDownloads)
            .HasDefaultValue(5);
        
        builder.Property(e => e.MaxConcurrentUploads)
            .HasDefaultValue(5);
        
        builder.Property(e => e.EnableDebugLogging)
            .HasDefaultValue(false);
    }
}