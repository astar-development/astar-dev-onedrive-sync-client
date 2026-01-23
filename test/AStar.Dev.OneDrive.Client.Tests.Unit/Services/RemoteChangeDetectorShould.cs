// using AStar.Dev.OneDrive.Client.Core.Models;
// using AStar.Dev.OneDrive.Client.Core.Models.Enums;
// using AStar.Dev.OneDrive.Client.Infrastructure.Services;
// using AStar.Dev.OneDrive.Client.Services;
// using Microsoft.Graph.Models;

// namespace AStar.Dev.OneDrive.Client.Tests.Unit.Services;

// public class RemoteChangeDetectorShould
// {
//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task DetectChangesInRootFolder()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         var file1 = new DriveItem { Id = "file1", Name = "doc1.txt", File = new FileObject(), Size = 100 };
//         var file2 = new DriveItem { Id = "file2", Name = "doc2.txt", File = new FileObject(), Size = 200 };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file1, file2]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata>? changes, var deltaLink) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);

//         changes.Count.ShouldBe(2);
//         changes.ShouldAllBe(c => c.AccountId == "acc1");
//         changes.ShouldAllBe(c => c.SyncStatus == FileSyncStatus.PendingDownload);
//         changes.ShouldContain(c => c.Name == "doc1.txt");
//         changes.ShouldContain(c => c.Name == "doc2.txt");
//         _ = deltaLink.ShouldNotBeNull();
//     }

//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task DetectChangesInNestedFolders()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         var subFolder = new DriveItem { Id = "folder1", Name = "SubFolder", Folder = new Core.Models.Folder() };
//         var file1 = new DriveItem { Id = "file1", Name = "root.txt", File = new FileObject(), Size = 100 };
//         var file2 = new DriveItem { Id = "file2", Name = "nested.txt", File = new FileObject(), Size = 200 };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file1, subFolder]);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "folder1", Arg.Any<CancellationToken>()).Returns([file2]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata>? changes, var deltaLink) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);

//         changes.Count.ShouldBe(2);
//         changes.ShouldContain(c => c.Name == "root.txt" && c.RelativePath == "/root.txt");
//         changes.ShouldContain(c => c.Name == "nested.txt" && c.RelativePath == "/SubFolder/nested.txt");
//     }

//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task SetCorrectMetadataForRemoteFiles()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         var lastModified = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);
//         var file = new DriveItem
//         {
//             Id = "file1",
//             Name = "document.txt",
//             File = new FileObject(),
//             Size = 1024,
//             LastModifiedDateTime = lastModified,
//             CTag = "ctag123",
//             ETag = "etag456"
//         };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata>? changes, _) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);

//         changes.Count.ShouldBe(1);
//         FileMetadata metadata = changes[0];
//         metadata.Id.ShouldBe("file1");
//         metadata.Name.ShouldBe("document.txt");
//         metadata.Size.ShouldBe(1024);
//         metadata.LastModifiedUtc.ShouldBe(lastModified.UtcDateTime);
//         metadata.CTag.ShouldBe("ctag123");
//         metadata.ETag.ShouldBe("etag456");
//         metadata.SyncStatus.ShouldBe(FileSyncStatus.PendingDownload);
//         metadata.LastSyncDirection.ShouldBe(SyncDirection.Download);
//         metadata.LocalPath.ShouldBeEmpty();
//         metadata.LocalHash.ShouldBeNull();
//     }

//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task ReturnEmptyChangesForEmptyFolder()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata>? changes, var deltaLink) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);

//         changes.ShouldBeEmpty();
//         _ = deltaLink.ShouldNotBeNull();
//     }

//     [Fact]
//     public async Task ReturnEmptyChangesWhenRootNotFound()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DriveItem?)null);
//         var detector = new RemoteChangeDetector(mockClient);

//         _ = await Should.ThrowAsync<InvalidOperationException>(async () => await detector.DetectChangesAsync("acc1", "/", null));
//     }

//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task GenerateNewDeltaLinkAfterScan()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata> _, var deltaLink1) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);
//         await Task.Delay(1100, TestContext.Current.CancellationToken); // Wait for second to change in timestamp
//         (IReadOnlyList<FileMetadata> _, var deltaLink2) = await detector.DetectChangesAsync("acc1", "/", deltaLink1, TestContext.Current.CancellationToken);

//         _ = deltaLink1.ShouldNotBeNull();
//         _ = deltaLink2.ShouldNotBeNull();
//         deltaLink1.ShouldNotBe(deltaLink2); // Should generate different tokens
//     }

//     [Fact]
//     public async Task HandleCancellation()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var detector = new RemoteChangeDetector(mockClient);
//         using var cts = new CancellationTokenSource();
//         await cts.CancelAsync();

//         _ = await Should.ThrowAsync<OperationCanceledException>(async () => await detector.DetectChangesAsync("acc1", "/", null, cts.Token));
//     }

//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task SkipFoldersAndOnlyReturnFiles()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         var file = new DriveItem { Id = "file1", Name = "doc.txt", File = new FileObject() };
//         var folder = new DriveItem { Id = "folder1", Name = "Folder", Folder = new Core.Models.Folder() };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([file, folder]);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "folder1", Arg.Any<CancellationToken>()).Returns([]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata>? changes, _) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);

//         changes.Count.ShouldBe(1);
//         changes[0].Name.ShouldBe("doc.txt");
//     }

//     [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
//     public async Task HandleItemsWithoutRequiredProperties()
//     {
//         IGraphApiClient mockClient = Substitute.For<IGraphApiClient>();
//         var rootItem = new DriveItem { Id = "root", Name = "root", Folder = new Core.Models.Folder() };
//         var validFile = new DriveItem { Id = "file1", Name = "valid.txt", File = new FileObject() };
//         var fileWithoutId = new DriveItem { Id = null, Name = "noId.txt", File = new FileObject() };
//         var fileWithoutName = new DriveItem { Id = "file2", Name = null, File = new FileObject() };
//         _ = mockClient.GetDriveRootAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(rootItem);
//         _ = mockClient.GetDriveItemChildrenAsync(Arg.Any<string>(), "root", Arg.Any<CancellationToken>()).Returns([validFile, fileWithoutId, fileWithoutName]);
//         var detector = new RemoteChangeDetector(mockClient);

//         (IReadOnlyList<FileMetadata>? changes, _) = await detector.DetectChangesAsync("acc1", "/", null, TestContext.Current.CancellationToken);

//         changes.Count.ShouldBe(1);
//         changes[0].Name.ShouldBe("valid.txt");
//     }
// }
