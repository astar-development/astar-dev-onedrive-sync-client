using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Services.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Tests.Unit.Services;

public class RemoteChangeDetectorShould
{
    [Fact]
    public async Task DetectChangesInRootFolder()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        var file1 = new DriveItem { Id = "file1", Name = "doc1.txt", File = new(), Size = 100 };
        var file2 = new DriveItem { Id = "file2", Name = "doc2.txt", File = new(), Size = 200 };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file1, file2]);
        var detector = new RemoteChangeDetector(mockClient);

        var (changes, deltaLink) = await detector.DetectChangesAsync("acc1", "/", null);

        changes.Count.ShouldBe(2);
        changes.ShouldAllBe(c => c.AccountId == "acc1");
        changes.ShouldAllBe(c => c.SyncStatus == FileSyncStatus.PendingDownload);
        changes.ShouldContain(c => c.Name == "doc1.txt");
        changes.ShouldContain(c => c.Name == "doc2.txt");
        deltaLink.ShouldNotBeNull();
    }

    [Fact]
    public async Task DetectChangesInNestedFolders()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        var subFolder = new DriveItem { Id = "folder1", Name = "SubFolder", Folder = new Folder() };
        var file1 = new DriveItem { Id = "file1", Name = "root.txt", File = new(), Size = 100 };
        var file2 = new DriveItem { Id = "file2", Name = "nested.txt", File = new(), Size = 200 };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file1, subFolder]);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "folder1", Arg.Any<CancellationToken>()).Returns([file2]);
        var detector = new RemoteChangeDetector(mockClient);

        var (changes, deltaLink) = await detector.DetectChangesAsync("acc1", "/", null);

        changes.Count.ShouldBe(2);
        changes.ShouldContain(c => c.Name == "root.txt" && c.Path == "/root.txt");
        changes.ShouldContain(c => c.Name == "nested.txt" && c.Path == "/SubFolder/nested.txt");
    }

    [Fact]
    public async Task SetCorrectMetadataForRemoteFiles()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        var lastModified = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);
        var file = new DriveItem
        {
            Id = "file1",
            Name = "document.txt",
            File = new(),
            Size = 1024,
            LastModifiedDateTime = lastModified,
            CTag = "ctag123",
            ETag = "etag456"
        };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file]);
        var detector = new RemoteChangeDetector(mockClient);

        var (changes, _) = await detector.DetectChangesAsync("acc1", "/", null);

        changes.Count.ShouldBe(1);
        var metadata = changes[0];
        metadata.Id.ShouldBe("file1");
        metadata.Name.ShouldBe("document.txt");
        metadata.Size.ShouldBe(1024);
        metadata.LastModifiedUtc.ShouldBe(lastModified.UtcDateTime);
        metadata.CTag.ShouldBe("ctag123");
        metadata.ETag.ShouldBe("etag456");
        metadata.SyncStatus.ShouldBe(FileSyncStatus.PendingDownload);
        metadata.LastSyncDirection.ShouldBe(SyncDirection.Download);
        metadata.LocalPath.ShouldBeEmpty();
        metadata.LocalHash.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnEmptyChangesForEmptyFolder()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([]);
        var detector = new RemoteChangeDetector(mockClient);

        var (changes, deltaLink) = await detector.DetectChangesAsync("acc1", "/", null);

        changes.ShouldBeEmpty();
        deltaLink.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnEmptyChangesWhenRootNotFound()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DriveItem?)null);
        var detector = new RemoteChangeDetector(mockClient);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await detector.DetectChangesAsync("acc1", "/", null));
    }

    [Fact]
    public async Task GenerateNewDeltaLinkAfterScan()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([]);
        var detector = new RemoteChangeDetector(mockClient);

        var (_, deltaLink1) = await detector.DetectChangesAsync("acc1", "/", null);
        await Task.Delay(1100); // Wait for second to change in timestamp
        var (_, deltaLink2) = await detector.DetectChangesAsync("acc1", "/", deltaLink1);

        deltaLink1.ShouldNotBeNull();
        deltaLink2.ShouldNotBeNull();
        deltaLink1.ShouldNotBe(deltaLink2); // Should generate different tokens
    }

    [Fact]
    public async Task HandleCancellation()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var detector = new RemoteChangeDetector(mockClient);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await detector.DetectChangesAsync("acc1", "/", null, cts.Token));
    }

    [Fact]
    public async Task SkipFoldersAndOnlyReturnFiles()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        var file = new DriveItem { Id = "file1", Name = "doc.txt", File = new() };
        var folder = new DriveItem { Id = "folder1", Name = "Folder", Folder = new Folder() };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file, folder]);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "folder1", Arg.Any<CancellationToken>()).Returns([]);
        var detector = new RemoteChangeDetector(mockClient);

        var (changes, _) = await detector.DetectChangesAsync("acc1", "/", null);

        changes.Count.ShouldBe(1);
        changes[0].Name.ShouldBe("doc.txt");
    }

    [Fact]
    public async Task HandleItemsWithoutRequiredProperties()
    {
        var mockClient = Substitute.For<IGraphApiClient>();
        var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Folder() };
        var validFile = new DriveItem { Id = "file1", Name = "valid.txt", File = new() };
        var fileWithoutId = new DriveItem { Id = null, Name = "noId.txt", File = new() };
        var fileWithoutName = new DriveItem { Id = "file2", Name = null, File = new() };
        mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
        mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([validFile, fileWithoutId, fileWithoutName]);
        var detector = new RemoteChangeDetector(mockClient);

        var (changes, _) = await detector.DetectChangesAsync("acc1", "/", null);

        changes.Count.ShouldBe(1);
        changes[0].Name.ShouldBe("valid.txt");
    }
}
