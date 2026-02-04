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
        _ = builder.HasKey(e => e.Id);

        _ = builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        _ = builder.Property(e => e.LogLevel)
            .IsRequired();

        _ = builder.Property(e => e.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        _ = builder.Property(e => e.Properties)
            .HasColumnType("jsonb");

        _ = builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        _ = builder.HasIndex(e => new { e.AccountId, e.Timestamp })
            .HasDatabaseName("idx_applicationlogs_accountid_timestamp")
            .IsDescending(false, true); // Timestamp DESC for recent-first ordering

        _ = builder.HasIndex(e => e.LogLevel)
            .HasDatabaseName("idx_applicationlogs_loglevel");
    }
}
