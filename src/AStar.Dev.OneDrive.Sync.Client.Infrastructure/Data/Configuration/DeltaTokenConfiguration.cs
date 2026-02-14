using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public sealed class DeltaTokenConfiguration : IEntityTypeConfiguration<DeltaToken>
{
    public void Configure(EntityTypeBuilder<DeltaToken> builder)
    {
        _ = builder.ToTable("DeltaTokens");
        _ = builder.HasKey(t => t.Id);

        _ = builder.Property(e => e.Token).HasColumnType("TEXT");

        _ = builder.HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
