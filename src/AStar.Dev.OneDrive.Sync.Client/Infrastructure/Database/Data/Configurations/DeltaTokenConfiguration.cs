using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data.Configurations;

/// <summary>
/// Entity configuration for the DeltaToken entity.
/// </summary>
public class DeltaTokenConfiguration : IEntityTypeConfiguration<DeltaToken>
{
    /// <summary>
    /// Configures the DeltaToken entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<DeltaToken> builder)
    {
        _ = builder.HasKey(e => e.Id);

        _ = builder.Property(e => e.Id)
            .IsRequired();

        _ = builder.Property(e => e.HashedAccountId)
            .IsRequired();

        _ = builder.Property(e => e.DriveName)
            .IsRequired();

        // Foreign key relationship with CASCADE DELETE for GDPR-compliant account deletion
        _ = builder.HasOne<Account>()
            .WithMany(a => a.DeltaTokens)
            .HasForeignKey("AccountId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
