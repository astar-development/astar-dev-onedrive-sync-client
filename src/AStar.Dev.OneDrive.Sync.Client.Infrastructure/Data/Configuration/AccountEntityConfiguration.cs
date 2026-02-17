using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Configuration;

public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        _ = builder.HasKey(e => e.HashedAccountId);
        _ = builder.Property(e => e.DisplayName).IsRequired();
        _ = builder.Property(e => e.LocalSyncPath).IsRequired();
        _ = builder.HasIndex(e => e.LocalSyncPath).IsUnique();

        _ = builder.Property(e => e.HashedAccountId).IsRequired()
            .HasConversion(SqliteTypeConverters.HashedAccountIdToString)
            .HasColumnType("TEXT");

        _ = builder.HasData(AccountEntity.CreateSystemAccount());
    }
}
