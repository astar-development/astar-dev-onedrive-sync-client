using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the ApplicationLog entity.
/// </summary>
public class ApplicationLogConfiguration : IEntityTypeConfiguration<ApplicationLog>
{
    /// <summary>
    /// Configures the ApplicationLog entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ApplicationLog> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();
        
        builder.Property(e => e.LogLevel)
            .IsRequired();
        
        builder.Property(e => e.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        
        // Configure JSONB column type for PostgreSQL
        builder.Property(e => e.Properties)
            .HasColumnType("jsonb");
        
        // Configure foreign key relationship (nullable for global logs)
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
        
        // Create indexes for log viewer paging and filtering
        builder.HasIndex(e => new { e.AccountId, e.Timestamp })
            .HasDatabaseName("idx_applicationlogs_accountid_timestamp")
            .IsDescending(false, true); // Timestamp DESC for recent-first ordering
        
        builder.HasIndex(e => e.LogLevel)
            .HasDatabaseName("idx_applicationlogs_loglevel");
    }
}