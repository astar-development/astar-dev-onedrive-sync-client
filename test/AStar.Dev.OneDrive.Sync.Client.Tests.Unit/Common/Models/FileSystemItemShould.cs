using AStar.Dev.OneDrive.Sync.Client.Common.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Common.Models;

public class FileSystemItemShould
{
    [Fact]
    public void CreateFileSystemItemWithValidProperties()
    {
        const string id = "item-123";
        const string hashedAccountId = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        const string driveItemId = "01BYE5RZ6QN3VTHOCKZFDNVD9BSSGGFJ7";
        const string name = "Document.txt";
        const string path = "/Documents/Document.txt";
        const bool isFolder = false;
        const bool isSelected = true;

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
        var item = new FileSystemItem { Name = null };

        item.Name.ShouldBeNull();
    }

    [Fact]
    public void AllowNullPath()
    {
        var item = new FileSystemItem { Path = null };

        item.Path.ShouldBeNull();
    }

    [Fact]
    public void HaveDefaultIsSelectedFalse()
    {
        var item = new FileSystemItem();

        item.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void HaveDefaultSyncStatusNone()
    {
        var item = new FileSystemItem();

        item.SyncStatus.ShouldBe(SyncStatus.None);
    }

    [Fact]
    public void HaveDefaultLastSyncDirectionNone()
    {
        var item = new FileSystemItem();

        item.LastSyncDirection.ShouldBe(SyncDirection.None);
    }

    [Fact]
    public void AllowSettingSyncStatus()
    {
        var item = new FileSystemItem { SyncStatus = SyncStatus.Synced };

        item.SyncStatus.ShouldBe(SyncStatus.Synced);
    }

    [Fact]
    public void AllowSettingLastSyncDirection()
    {
        var item = new FileSystemItem { LastSyncDirection = SyncDirection.Upload };

        item.LastSyncDirection.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public void AllowNullRemoteHash()
    {
        var item = new FileSystemItem { RemoteHash = null };

        item.RemoteHash.ShouldBeNull();
    }

    [Fact]
    public void AllowNullLocalHash()
    {
        var item = new FileSystemItem { LocalHash = null };

        item.LocalHash.ShouldBeNull();
    }

    [Fact]
    public void AllowSettingHashValues()
    {
        const string remoteHash = "abc123def456";
        const string localHash = "xyz789uvw012";

        var item = new FileSystemItem
        {
            RemoteHash = remoteHash,
            LocalHash = localHash
        };

        item.RemoteHash.ShouldBe(remoteHash);
        item.LocalHash.ShouldBe(localHash);
    }
}
