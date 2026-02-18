using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data.Entities;

public class DriveItemEntitySelectionUpdateShould
{
    [Fact]
    public void UpdateSelectionStatusCorrectly()
    {
        var originalEntity = new DriveItemEntity(
            new HashedAccountId("hashed-account-id"),
            "drive-item-id",
            "relative/path/to/item",
            "etag-value",
            "ctag-value",
            1024,
            DateTimeOffset.UtcNow,
            isFolder: false,
            isDeleted: false,
            isSelected: false);

        DriveItemEntity updatedEntity = originalEntity.WithUpdatedSelection(true);

        updatedEntity.IsSelected!.Value.ShouldBeTrue();
        updatedEntity.HashedAccountId.ShouldBe(originalEntity.HashedAccountId);
        updatedEntity.DriveItemId.ShouldBe(originalEntity.DriveItemId);
        updatedEntity.RelativePath.ShouldBe(originalEntity.RelativePath);
        updatedEntity.ETag.ShouldBe(originalEntity.ETag);
        updatedEntity.CTag.ShouldBe(originalEntity.CTag);
        updatedEntity.Size.ShouldBe(originalEntity.Size);
        updatedEntity.LastModifiedUtc.ShouldBe(originalEntity.LastModifiedUtc);
        updatedEntity.IsFolder.ShouldBe(originalEntity.IsFolder);
        updatedEntity.IsDeleted.ShouldBe(originalEntity.IsDeleted);
    }
}
