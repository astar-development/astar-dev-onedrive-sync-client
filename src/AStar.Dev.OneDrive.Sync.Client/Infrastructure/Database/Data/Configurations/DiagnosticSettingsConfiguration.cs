using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the DiagnosticSettings entity.
/// </summary>
public class DiagnosticSettingsConfiguration : IEntityTypeConfiguration<DiagnosticSettings>
{
    /// <summary>
    /// Configures the DiagnosticSettings entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<DiagnosticSettings> builder)
    {
        _ = builder.HasKey(e => e.Id);

        _ = builder.Property(e => e.Id)
            .IsRequired();

        _ = builder.Property(e => e.HashedAccountId)
            .IsRequired();

        _ = builder.HasIndex(e => e.HashedAccountId)
            .IsUnique();

        _ = builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);
    }
}
