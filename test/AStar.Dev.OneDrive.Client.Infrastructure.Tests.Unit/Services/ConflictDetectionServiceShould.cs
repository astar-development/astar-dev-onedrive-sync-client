using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

public class ConflictDetectionServiceShould
{
    private const string AccountId = "test-account-id";

    [Fact]
    public async Task DetectConflictForKnownFile_WhenBothLocalAndRemoteChanged()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "newETag", "newCTag", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var existingFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "oldETag", "oldCTag", 200,
            DateTimeOffset.UtcNow.AddHours(-2), false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var localFile = new FileMetadata(
            "item1", AccountId, "test.txt", "/test.txt", 200,
            DateTimeOffset.UtcNow.AddMinutes(-5), @"C:\Sync\test.txt");
        var localFilesDict = new Dictionary<string, FileMetadata>
        {
            ["/test.txt"] = localFile
        };
        _ = syncConflictRepo.GetByFilePathAsync(AccountId, "/test.txt", Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        (var hasConflict, FileMetadata? fileToDownload) = await service.CheckKnownFileConflictAsync(AccountId, remoteFile, existingFile, localFilesDict, null, null, CancellationToken.None);

        hasConflict.ShouldBeTrue();
        fileToDownload.ShouldBeNull();
        await syncConflictRepo.Received(1).AddAsync(Arg.Any<SyncConflict>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotDetectConflictForKnownFile_WhenOnlyRemoteChanged()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "newETag", "newCTag", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var existingFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "oldETag", "oldCTag", 200,
            DateTimeOffset.UtcNow.AddHours(-2), false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var localFile = new FileMetadata(
            "item1", AccountId, "test.txt", "/test.txt", 200,
            existingFile.LastModifiedUtc, @"C:\Sync\test.txt");
        var localFilesDict = new Dictionary<string, FileMetadata>
        {
            ["/test.txt"] = localFile
        };

        (var hasConflict, FileMetadata? fileToDownload) = await service.CheckKnownFileConflictAsync(AccountId, remoteFile, existingFile, localFilesDict, @"C:\Sync", null, CancellationToken.None);

        hasConflict.ShouldBeFalse();
        _ = fileToDownload.ShouldNotBeNull();
        fileToDownload!.DriveItemId.ShouldBe("item1");
        await syncConflictRepo.DidNotReceive().AddAsync(Arg.Any<SyncConflict>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotDetectConflict_WhenRemoteHasNotChanged()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "sameETag", "sameCTag", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var existingFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "sameETag", "sameCTag", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var localFilesDict = new Dictionary<string, FileMetadata>();

        (var hasConflict, FileMetadata? fileToDownload) = await service.CheckKnownFileConflictAsync(AccountId, remoteFile, existingFile, localFilesDict, null, null, CancellationToken.None);

        hasConflict.ShouldBeFalse();
        fileToDownload.ShouldBeNull();
        await syncConflictRepo.DidNotReceive().AddAsync(Arg.Any<SyncConflict>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectConflictForFirstSync_WhenLocalAndRemoteFilesDiffer()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            null, null, FileSyncStatus.SyncOnly, SyncDirection.None);
        var localFile = new FileMetadata(
            "", AccountId, "test.txt", "/test.txt", 150, // Different size
            DateTimeOffset.UtcNow.AddMinutes(-10), @"C:\Sync\test.txt");
        var localFilesDict = new Dictionary<string, FileMetadata>
        {
            ["/test.txt"] = localFile
        };
        _ = syncConflictRepo.GetByFilePathAsync(AccountId, "/test.txt", Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        (var hasConflict, FileMetadata? fileToDownload, FileMetadata? matchedFile) = await service.CheckFirstSyncFileConflictAsync(AccountId, remoteFile, localFilesDict, null, null, CancellationToken.None);

        hasConflict.ShouldBeTrue();
        fileToDownload.ShouldBeNull();
        matchedFile.ShouldBeNull();
        await syncConflictRepo.Received(1).AddAsync(Arg.Any<SyncConflict>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchFilesForFirstSync_WhenSizeAndTimestampMatch()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            timestamp, false, false, true, null, "test.txt",
            null, null, FileSyncStatus.SyncOnly, SyncDirection.None);
        var localFile = new FileMetadata(
            "", AccountId, "test.txt", "/test.txt", 200,
            timestamp.AddSeconds(30), @"C:\Sync\test.txt"); // Within 60 second threshold
        var localFilesDict = new Dictionary<string, FileMetadata>
        {
            ["/test.txt"] = localFile
        };

        (var hasConflict, FileMetadata? fileToDownload, FileMetadata? matchedFile) = await service.CheckFirstSyncFileConflictAsync(
            AccountId, remoteFile, localFilesDict, null, null, CancellationToken.None);

        hasConflict.ShouldBeFalse();
        fileToDownload.ShouldBeNull();
        _ = matchedFile.ShouldNotBeNull();
        matchedFile!.DriveItemId.ShouldBe("item1");
        matchedFile.CTag.ShouldBe("ctag1");
        matchedFile.SyncStatus.ShouldBe(FileSyncStatus.Synced);
        await syncConflictRepo.DidNotReceive().AddAsync(Arg.Any<SyncConflict>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadNewFileForFirstSync_WhenLocalFileDoesNotExist()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            null, null, FileSyncStatus.SyncOnly, SyncDirection.None);
        var localFilesDict = new Dictionary<string, FileMetadata>();
        (var hasConflict, FileMetadata? fileToDownload, FileMetadata? matchedFile) = await service.CheckFirstSyncFileConflictAsync(AccountId, remoteFile, localFilesDict, @"C:\Sync", null, CancellationToken.None);

        hasConflict.ShouldBeFalse();
        _ = fileToDownload.ShouldNotBeNull();
        fileToDownload!.DriveItemId.ShouldBe("item1");
        fileToDownload.LocalPath.ShouldBe(Path.Combine(@"C:\Sync", "test.txt"));
        matchedFile.ShouldBeNull();
        await syncConflictRepo.DidNotReceive().AddAsync(Arg.Any<SyncConflict>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckLocalFileChanged_ReturnTrue_WhenTimestampDiffers()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var existingFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            DateTimeOffset.UtcNow.AddHours(-1), false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var localFile = new FileMetadata(
            "item1", AccountId, "test.txt", "/test.txt", 200,
            DateTimeOffset.UtcNow, @"C:\Sync\test.txt"); // Different timestamp
        var localFilesDict = new Dictionary<string, FileMetadata>
        {
            ["/test.txt"] = localFile
        };

        var hasChanged = service.CheckIfLocalFileHasChanged("/test.txt", existingFile, localFilesDict);

        hasChanged.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckLocalFileChanged_ReturnTrue_WhenSizeDiffers()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        var existingFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            timestamp, false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var localFile = new FileMetadata(
            "item1", AccountId, "test.txt", "/test.txt", 250, // Different size
            timestamp, @"C:\Sync\test.txt");
        var localFilesDict = new Dictionary<string, FileMetadata>
        {
            ["/test.txt"] = localFile
        };

        var hasChanged = service.CheckIfLocalFileHasChanged("/test.txt", existingFile, localFilesDict);

        hasChanged.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckLocalFileChanged_ReturnFalse_WhenFileNotInLocalDict()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var existingFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            @"C:\Sync\test.txt", null, FileSyncStatus.Synced, SyncDirection.Download);
        var localFilesDict = new Dictionary<string, FileMetadata>();

        var hasChanged = service.CheckIfLocalFileHasChanged("/test.txt", existingFile, localFilesDict);

        hasChanged.ShouldBeFalse();
    }

    [Fact]
    public async Task RecordConflict_ShouldLogToRepositories()
    {
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IDriveItemsRepository driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var service = new ConflictDetectionService(syncConflictRepo, fileOperationLogRepo, driveItemsRepo);
        var remoteFile = new DriveItemEntity(
            AccountId, "item1", "/test.txt", "etag1", "ctag1", 200,
            DateTimeOffset.UtcNow, false, false, true, null, "test.txt",
            null, null, FileSyncStatus.SyncOnly, SyncDirection.None);
        var localFile = new FileMetadata(
            "item1", AccountId, "test.txt", "/test.txt", 150,
            DateTimeOffset.UtcNow.AddMinutes(-5), @"C:\Sync\test.txt");
        _ = syncConflictRepo.GetByFilePathAsync(AccountId, "/test.txt", Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        await service.RecordSyncConflictAsync(AccountId, remoteFile, localFile, "session1", CancellationToken.None);

        await syncConflictRepo.Received(1).AddAsync(
            Arg.Is<SyncConflict>(c =>
                c.AccountId == AccountId &&
                c.FilePath == "/test.txt" &&
                c.IsResolved == false),
            Arg.Any<CancellationToken>());

        await fileOperationLogRepo.Received(1).AddAsync(
            Arg.Any<FileOperationLog>(),
            Arg.Any<CancellationToken>());

        await driveItemsRepo.Received(1).SaveBatchAsync(
            Arg.Is<IEnumerable<FileMetadata>>(list =>
                list.Count() == 1 &&
                list.First().SyncStatus == FileSyncStatus.PendingDownload),
            Arg.Any<CancellationToken>());
    }
}
