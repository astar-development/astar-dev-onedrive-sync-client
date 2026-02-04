using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the SyncHistory entity.
/// </summary>
public class SyncHistoryConfiguration : IEntityTypeConfiguration<SyncHistory>
{
    /// <summary>
    /// Configures the SyncHistory entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<SyncHistory> builder)
    {
        _ = builder.HasKey(e => e.Id);

        _ = builder.Property(e => e.Id)
            .IsRequired();

        _ = builder.Property(e => e.HashedAccountId)
            .IsRequired();
    }
}
