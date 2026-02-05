using AStar.Dev.OneDrive.Sync.Client.Common.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Common.Models;

public class FileSystemItemShould
{
    [Fact]
    public void CreateFileSystemItemWithValidProperties()
    {
        // Arrange
        const string id = "item-123";
        const string hashedAccountId = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        const string driveItemId = "01BYE5RZ6QN3VTHOCKZFDNVD9BSSGGFJ7";
        const string name = "Document.txt";
        const string path = "/Documents/Document.txt";
        const bool isFolder = false;
        const bool isSelected = true;

        // Act
        var item = new FileSystemItem
        {
            Id = id,
            HashedAccountId = hashedAccountId,
            DriveItemId = driveItemId,
            Name = name,
            Path = path,
            IsFolder = isFolder,
            IsSelected = isSelected
        };

        // Assert
        item.Id.ShouldBe(id);
        item.HashedAccountId.ShouldBe(hashedAccountId);
        item.DriveItemId.ShouldBe(driveItemId);
        item.Name.ShouldBe(name);
        item.Path.ShouldBe(path);
        item.IsFolder.ShouldBe(isFolder);
        item.IsSelected.ShouldBe(isSelected);
    }

    [Fact]
    public void ThrowArgumentExceptionForNullId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { Id = null! }; });
        ex.Message.ShouldContain("Id");
    }

    [Fact]
    public void ThrowArgumentExceptionForEmptyId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { Id = string.Empty }; });
        ex.Message.ShouldContain("Id");
    }

    [Fact]
    public void ThrowArgumentExceptionForWhitespaceId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { Id = "   " }; });
        ex.Message.ShouldContain("Id");
    }

    [Fact]
    public void ThrowArgumentExceptionForNullHashedAccountId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { HashedAccountId = null! }; });
        ex.Message.ShouldContain("HashedAccountId");
    }

    [Fact]
    public void ThrowArgumentExceptionForEmptyHashedAccountId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { HashedAccountId = string.Empty }; });
        ex.Message.ShouldContain("HashedAccountId");
    }

    [Fact]
    public void ThrowArgumentExceptionForWhitespaceHashedAccountId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { HashedAccountId = "   " }; });
        ex.Message.ShouldContain("HashedAccountId");
    }

    [Fact]
    public void ThrowArgumentExceptionForNullDriveItemId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { DriveItemId = null! }; });
        ex.Message.ShouldContain("DriveItemId");
    }

    [Fact]
    public void ThrowArgumentExceptionForEmptyDriveItemId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { DriveItemId = string.Empty }; });
        ex.Message.ShouldContain("DriveItemId");
    }

    [Fact]
    public void ThrowArgumentExceptionForWhitespaceDriveItemId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new FileSystemItem { DriveItemId = "   " }; });
        ex.Message.ShouldContain("DriveItemId");
    }

    [Fact]
    public void AllowNullName()
    {
        // Act
        var item = new FileSystemItem { Name = null };

        // Assert
        item.Name.ShouldBeNull();
    }

    [Fact]
    public void AllowNullPath()
    {
        // Act
        var item = new FileSystemItem { Path = null };

        // Assert
        item.Path.ShouldBeNull();
    }

    [Fact]
    public void HaveDefaultIsSelectedFalse()
    {
        // Act
        var item = new FileSystemItem();

        // Assert
        item.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void HaveDefaultSyncStatusNone()
    {
        // Act
        var item = new FileSystemItem();

        // Assert
        item.SyncStatus.ShouldBe(SyncStatus.None);
    }

    [Fact]
    public void HaveDefaultLastSyncDirectionNone()
    {
        // Act
        var item = new FileSystemItem();

        // Assert
        item.LastSyncDirection.ShouldBe(SyncDirection.None);
    }

    [Fact]
    public void AllowSettingSyncStatus()
    {
        // Act
        var item = new FileSystemItem { SyncStatus = SyncStatus.Synced };

        // Assert
        item.SyncStatus.ShouldBe(SyncStatus.Synced);
    }

    [Fact]
    public void AllowSettingLastSyncDirection()
    {
        // Act
        var item = new FileSystemItem { LastSyncDirection = SyncDirection.Upload };

        // Assert
        item.LastSyncDirection.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public void AllowNullRemoteHash()
    {
        // Act
        var item = new FileSystemItem { RemoteHash = null };

        // Assert
        item.RemoteHash.ShouldBeNull();
    }

    [Fact]
    public void AllowNullLocalHash()
    {
        // Act
        var item = new FileSystemItem { LocalHash = null };

        // Assert
        item.LocalHash.ShouldBeNull();
    }

    [Fact]
    public void AllowSettingHashValues()
    {
        // Arrange
        const string remoteHash = "abc123def456";
        const string localHash = "xyz789uvw012";

        // Act
        var item = new FileSystemItem
        {
            RemoteHash = remoteHash,
            LocalHash = localHash
        };

        // Assert
        item.RemoteHash.ShouldBe(remoteHash);
        item.LocalHash.ShouldBe(localHash);
    }
}
