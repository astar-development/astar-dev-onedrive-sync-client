using AStarOneDriveClient.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarOneDriveClient.Data.Configuration;

public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.HasKey(e => e.AccountId);
        builder.Property(e => e.AccountId).IsRequired();
        builder.Property(e => e.DisplayName).IsRequired();
        builder.Property(e => e.LocalSyncPath).IsRequired();
        builder.HasIndex(e => e.LocalSyncPath).IsUnique();
    }
}
