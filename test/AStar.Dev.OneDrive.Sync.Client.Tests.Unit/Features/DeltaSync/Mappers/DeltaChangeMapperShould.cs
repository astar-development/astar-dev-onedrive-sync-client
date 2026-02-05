using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Mappers;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.DeltaSync.Mappers;

public class DeltaChangeMapperShould
{
    private const string TestHashedAccountId = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Fact]
    public void ThrowArgumentNullExceptionWhenChangeIsNull()
        => Should.Throw<ArgumentNullException>(() => ((DeltaChange)null!).ToFileSystemItem(TestHashedAccountId));

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsNull()
    {
        DeltaChange change = CreateTestDeltaChange();
        Should.Throw<ArgumentException>(() => change.ToFileSystemItem(null!));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsEmpty()
    {
        DeltaChange change = CreateTestDeltaChange();
        Should.Throw<ArgumentException>(() => change.ToFileSystemItem(string.Empty));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsWhitespace()
    {
        DeltaChange change = CreateTestDeltaChange();
        Should.Throw<ArgumentException>(() => change.ToFileSystemItem("   "));
    }

    [Fact]
    public void MapAddedChangeToFileSystemItemWithPendingDownloadStatus()
    {
        var change = new DeltaChange
        {
            DriveItemId = "item-123",
            Name = "newfile.txt",
            Path = "/drive/root:/Documents",
            IsFolder = false,
            ChangeType = ChangeType.Added,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = "abc123"
        };

        var result = change.ToFileSystemItem(TestHashedAccountId);

        result.DriveItemId.ShouldBe("item-123");
        result.Name.ShouldBe("newfile.txt");
        result.Path.ShouldBe("/drive/root:/Documents");
        result.IsFolder.ShouldBe(false);
        result.HashedAccountId.ShouldBe(TestHashedAccountId);
        result.RemoteHash.ShouldBe("abc123");
        result.SyncStatus.ShouldBe(SyncStatus.PendingDownload);
        result.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void MapDeletedChangeToFileSystemItemWithPendingDownloadStatus()
    {
        var change = new DeltaChange
        {
            DriveItemId = "item-456",
            Name = "deleted.txt",
            Path = "/drive/root:",
            IsFolder = false,
            ChangeType = ChangeType.Deleted,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = null
        };

        var result = change.ToFileSystemItem(TestHashedAccountId);

        result.SyncStatus.ShouldBe(SyncStatus.PendingDownload);
        result.DriveItemId.ShouldBe("item-456");
    }

    [Fact]
    public void MapModifiedChangeWithDifferentHashToPendingDownload()
    {
        var existingItem = new FileSystemItem
        {
            Id = "existing-id",
            HashedAccountId = TestHashedAccountId,
            DriveItemId = "item-789",
            RemoteHash = "oldhash123",
            LocalPath = "/local/path/file.txt"
        };

        var change = new DeltaChange
        {
            DriveItemId = "item-789",
            Name = "modified.txt",
            Path = "/drive/root:",
            IsFolder = false,
            ChangeType = ChangeType.Modified,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = "newhash456"
        };

        var result = change.ToFileSystemItem(TestHashedAccountId, existingItem);

        result.SyncStatus.ShouldBe(SyncStatus.PendingDownload);
        result.Id.ShouldBe("existing-id");
        result.LocalPath.ShouldBe("/local/path/file.txt");
    }

    [Fact]
    public void MapModifiedChangeWithSameHashToSyncedStatus()
    {
        var existingItem = new FileSystemItem
        {
            Id = "existing-id",
            HashedAccountId = TestHashedAccountId,
            DriveItemId = "item-999",
            RemoteHash = "samehash123",
            LocalPath = "/local/path/file.txt"
        };

        var change = new DeltaChange
        {
            DriveItemId = "item-999",
            Name = "unchanged.txt",
            Path = "/drive/root:",
            IsFolder = false,
            ChangeType = ChangeType.Modified,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = "samehash123"
        };

        var result = change.ToFileSystemItem(TestHashedAccountId, existingItem);

        result.SyncStatus.ShouldBe(SyncStatus.Synced);
        result.Id.ShouldBe("existing-id");
    }

    [Fact]
    public void MapFolderChangeIgnoresHashComparison()
    {
        var existingItem = new FileSystemItem
        {
            Id = "folder-id",
            HashedAccountId = TestHashedAccountId,
            DriveItemId = "folder-123",
            IsFolder = true,
            RemoteHash = "oldhash"
        };

        var change = new DeltaChange
        {
            DriveItemId = "folder-123",
            Name = "MyFolder",
            Path = "/drive/root:",
            IsFolder = true,
            ChangeType = ChangeType.Modified,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = "newhash"
        };

        var result = change.ToFileSystemItem(TestHashedAccountId, existingItem);

        result.SyncStatus.ShouldBe(SyncStatus.Synced);
    }

    [Fact]
    public void MapChangeWithNullRemoteHashToPendingDownload()
    {
        var existingItem = new FileSystemItem
        {
            Id = "existing-id",
            HashedAccountId = TestHashedAccountId,
            DriveItemId = "item-111",
            RemoteHash = "oldhash"
        };

        var change = new DeltaChange
        {
            DriveItemId = "item-111",
            Name = "file.txt",
            Path = "/drive/root:",
            IsFolder = false,
            ChangeType = ChangeType.Modified,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = null
        };

        var result = change.ToFileSystemItem(TestHashedAccountId, existingItem);

        result.SyncStatus.ShouldBe(SyncStatus.PendingDownload);
    }

    [Fact]
    public void PreserveExistingItemPropertiesWhenMappingWithExistingItem()
    {
        var existingItem = new FileSystemItem
        {
            Id = "preserve-id",
            HashedAccountId = TestHashedAccountId,
            DriveItemId = "item-222",
            LocalPath = "/local/preserved/path.txt",
            LocalModifiedAt = DateTime.UtcNow.AddDays(-1),
            LocalHash = "localhash123",
            LastSyncDirection = SyncDirection.Download
        };

        var change = new DeltaChange
        {
            DriveItemId = "item-222",
            Name = "updated.txt",
            Path = "/drive/root:",
            IsFolder = false,
            ChangeType = ChangeType.Modified,
            RemoteModifiedAt = DateTime.UtcNow,
            RemoteHash = "newhash"
        };

        var result = change.ToFileSystemItem(TestHashedAccountId, existingItem);

        result.Id.ShouldBe("preserve-id");
        result.LocalPath.ShouldBe("/local/preserved/path.txt");
        result.LocalHash.ShouldBe("localhash123");
        result.LastSyncDirection.ShouldBe(SyncDirection.Download);
        result.LocalModifiedAt.ShouldBe(existingItem.LocalModifiedAt);
    }

    [Fact]
    public void GenerateNewIdWhenNoExistingItem()
    {
        DeltaChange change = CreateTestDeltaChange();

        var result = change.ToFileSystemItem(TestHashedAccountId);

        result.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(result.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public void MapRemoteModifiedAtFromChange()
    {
        DateTime modifiedTime = DateTime.UtcNow.AddHours(-2);
        var change = new DeltaChange
        {
            DriveItemId = "item-333",
            Name = "file.txt",
            Path = "/drive/root:",
            IsFolder = false,
            ChangeType = ChangeType.Modified,
            RemoteModifiedAt = modifiedTime,
            RemoteHash = "hash123"
        };

        var result = change.ToFileSystemItem(TestHashedAccountId);

        result.RemoteModifiedAt.ShouldBe(modifiedTime);
    }

    private static DeltaChange CreateTestDeltaChange() => new()
    {
        DriveItemId = "test-item",
        Name = "test.txt",
        Path = "/drive/root:",
        IsFolder = false,
        ChangeType = ChangeType.Added,
        RemoteModifiedAt = DateTime.UtcNow,
        RemoteHash = "testhash"
    };
}
