using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data.Entities;

public class DriveItemEntityShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        var driveItemEntity = new DriveItemEntity(
            new HashedAccountId("hashed-account-id"),
            "drive-item-id",
            "relative/path/to/item",
            "etag-value",
            "ctag-value",
            1024,
            currentTime,
            isFolder: false,
            isDeleted: false,
            isSelected: true,
            remoteHash: "remote-hash-value",
            name: "Test File.txt",
            localPath: "/local/path/to/item",
            localHash: "local-hash-value",
            syncStatus: FileSyncStatus.Synced,
            lastSyncDirection: SyncDirection.Upload);

        driveItemEntity.HashedAccountId.Value.ShouldBe("hashed-account-id");
        driveItemEntity.DriveItemId.ShouldBe("drive-item-id");
        driveItemEntity.RelativePath.ShouldBe("relative/path/to/item");
        driveItemEntity.ETag.ShouldBe("etag-value");
        driveItemEntity.CTag.ShouldBe("ctag-value");
        driveItemEntity.Size.ShouldBe(1024);
        driveItemEntity.LastModifiedUtc.ShouldBe(currentTime);
        driveItemEntity.IsFolder.ShouldBeFalse();
        driveItemEntity.IsDeleted.ShouldBeFalse();
        driveItemEntity.IsSelected!.Value.ShouldBeTrue();
        driveItemEntity.RemoteHash.ShouldBe("remote-hash-value");
        driveItemEntity.Name.ShouldBe("Test File.txt");
        driveItemEntity.LocalPath.ShouldBe("/local/path/to/item");
        driveItemEntity.LocalHash.ShouldBe("local-hash-value");
        driveItemEntity.SyncStatus.ShouldBe(FileSyncStatus.Synced);
        driveItemEntity.LastSyncDirection.ShouldBe(SyncDirection.Upload);
    }
}
