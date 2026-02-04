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
        _ = builder.HasKey(e => e.Id);

        _ = builder.HasIndex(e => e.HashedEmail)
            .IsUnique();

        _ = builder.Property(e => e.Id)
            .IsRequired();

        _ = builder.Property(e => e.HashedEmail)
            .IsRequired();

        _ = builder.Property(e => e.HashedAccountId)
            .IsRequired();

        _ = builder.Property(e => e.MaxConcurrent)
            .HasDefaultValue(5);

        _ = builder.Property(e => e.DebugLoggingEnabled)
            .HasDefaultValue(false);

        _ = builder.Property(e => e.IsAdmin)
            .HasDefaultValue(false);

        _ = builder.Property(e => e.MaxBandwidthKBps);

        _ = builder.Property(e => e.CreatedAt);

        _ = builder.Property(e => e.UpdatedAt);
    }
}
