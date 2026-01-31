using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configuration;

public sealed class WindowPreferencesEntityConfiguration : IEntityTypeConfiguration<WindowPreferencesEntity>
{
    public void Configure(EntityTypeBuilder<WindowPreferencesEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Width).HasDefaultValue(800);
        _ = builder.Property(e => e.Height).HasDefaultValue(600);
    }
}
