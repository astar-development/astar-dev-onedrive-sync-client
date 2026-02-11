using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.OneDriveServices;
using Microsoft.Graph.Models;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services.OneDriveServices;

public class FileTransferServiceShould
{
    [Fact]
    public async Task ExecuteUploadsAsync_SuccessfullyUploadSingleFile()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToUpload = new List<FileMetadata>
        {
            new("", accountId, "test.txt", "/Documents/test.txt", 100, DateTime.UtcNow, @"C:\Sync\Documents\test.txt",
                false, false, false, null, null, null, "hash1", FileSyncStatus.PendingUpload, SyncDirection.None)
        };

        var uploadedItem = new DriveItem { Id = "item-123", Name = "test.txt", CTag = "ctag-1", ETag = "etag-1", LastModifiedDateTime = DateTimeOffset.UtcNow };
        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(uploadedItem));

        var progressCalled = false;
        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => progressCalled = true;

        using var cts = new CancellationTokenSource();
        var (completedFiles, completedBytes) = await service.ExecuteUploadsAsync(accountId, existingItems, filesToUpload, maxParallelUploads: 3, conflictCount: 0, totalFiles: 1,
            totalBytes: 100, uploadBytes: 100, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        completedFiles.ShouldBe(1);
        completedBytes.ShouldBe(100);
        progressCalled.ShouldBeTrue();
        await mocks.GraphApiClient.Received(1).UploadFileAsync(accountId, @"C:\Sync\Documents\test.txt", "/Documents/test.txt", Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>());
        await mocks.DriveItemsRepository.Received().AddAsync(Arg.Is<FileMetadata>(f => f.Name == "test.txt" && f.SyncStatus == FileSyncStatus.PendingUpload), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteUploadsAsync_HandleMultipleFiles()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingFile = new DriveItemEntity(
            accountId: accountId, driveItemId: "existing-id", relativePath: "/Documents/test.txt", eTag: "etag-old", cTag: "ctag-old",
            size: 90, lastModifiedUtc: DateTime.UtcNow.AddDays(-1), isFolder: false, isDeleted: false, isSelected: false,
            remoteHash: null, name: "test.txt", localPath: @"C:\Sync\Documents\test.txt", localHash: "old-hash",
            syncStatus: FileSyncStatus.Synced, lastSyncDirection: SyncDirection.Upload);
        var existingItems = new List<DriveItemEntity> { existingFile }.AsReadOnly();

        var filesToUpload = Enumerable.Range(1, 10).Select(i => new FileMetadata(
            i <= 1 ? "existing-id" : "", accountId, $"file{i}.txt", $"/Documents/file{i}.txt", 100,
            DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", false, false, false, null, null, null, $"hash{i}",
            FileSyncStatus.PendingUpload, SyncDirection.None)).ToList();

        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new DriveItem
            {
                Id = $"uploaded-{Guid.CreateVersion7():N0}", Name = callInfo.ArgAt<string>(2).Split('/').Last(),
                CTag = "ctag", ETag = "etag", LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };
        using var cts = new CancellationTokenSource();
        var (completedFiles, completedBytes) = await service.ExecuteUploadsAsync(accountId, existingItems, filesToUpload, maxParallelUploads: 3, conflictCount: 0, totalFiles: 10,
            totalBytes: 1000, uploadBytes: 1000, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        completedFiles.ShouldBe(10);
        await mocks.GraphApiClient.Received(10).UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteUploadsAsync_HandleUploadFailureGracefully()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToUpload = new List<FileMetadata>
        {
            new("", accountId, "fail.txt", "/Documents/fail.txt", 100, DateTime.UtcNow, @"C:\Sync\Documents\fail.txt",
                false, false, false, null, null, null, "hash1", FileSyncStatus.PendingUpload, SyncDirection.None)
        };

        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns<DriveItem>(_ => throw new Exception("Upload failed"));

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };
        using var cts = new CancellationTokenSource();
        var (completedFiles, completedBytes) = await service.ExecuteUploadsAsync(accountId, existingItems, filesToUpload, maxParallelUploads: 3, conflictCount: 0, totalFiles: 1,
            totalBytes: 100, uploadBytes: 100, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        completedFiles.ShouldBe(1);
        completedBytes.ShouldBe(0);
        await mocks.DriveItemsRepository.Received().AddAsync(Arg.Is<FileMetadata>(f => f.SyncStatus == FileSyncStatus.Failed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDownloadsAsync_SuccessfullyDownloadSingleFile()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToDownload = new List<FileMetadata>
        {
            new("item-123", accountId, "test.txt", "/Documents/test.txt", 100, DateTime.UtcNow, @"C:\Sync\Documents\test.txt",
                false, false, false, "remote-hash", "ctag-1", "etag-1", null, FileSyncStatus.PendingDownload, SyncDirection.None)
        };

        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = mocks.LocalFileScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("downloaded-hash"));

        var progressCalled = false;
        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => progressCalled = true;

        using var cts = new CancellationTokenSource();
        var (completedFiles, completedBytes) = await service.ExecuteDownloadsAsync(accountId, existingItems, filesToDownload, maxParallelDownloads: 3, conflictCount: 0, totalFiles: 1,
            totalBytes: 100, uploadBytes: 0, downloadBytes: 100, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        completedFiles.ShouldBe(1);
        completedBytes.ShouldBe(100);
        progressCalled.ShouldBeTrue();
        await mocks.GraphApiClient.Received(1).DownloadFileAsync(accountId, "item-123", @"C:\Sync\Documents\test.txt", Arg.Any<CancellationToken>());
        await mocks.LocalFileScanner.Received(1).ComputeFileHashAsync(@"C:\Sync\Documents\test.txt", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDownloadsAsync_HandleMultipleFiles()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToDownload = Enumerable.Range(1, 10).Select(i => new FileMetadata(
            $"item-{i}", accountId, $"file{i}.txt", $"/Documents/file{i}.txt", 100, DateTime.UtcNow,
            $@"C:\Sync\Documents\file{i}.txt", false, false, false, "remote-hash", "ctag", "etag", null,
            FileSyncStatus.PendingDownload, SyncDirection.None)).ToList();

        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = mocks.LocalFileScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("downloaded-hash"));

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };
        using var cts = new CancellationTokenSource();
        var (completedFiles, completedBytes) = await service.ExecuteDownloadsAsync(accountId, existingItems, filesToDownload, maxParallelDownloads: 5, conflictCount: 0,
            totalFiles: 10, totalBytes: 1000, uploadBytes: 0, downloadBytes: 1000, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts,
            TestContext.Current.CancellationToken);

        completedFiles.ShouldBe(10);
        await mocks.GraphApiClient.Received(10).DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDownloadsAsync_HandleDownloadFailureGracefully()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToDownload = new List<FileMetadata>
        {
            new("item-fail", accountId, "fail.txt", "/Documents/fail.txt", 100, DateTime.UtcNow, @"C:\Sync\Documents\fail.txt",
                false, false, false, "remote-hash", "ctag-1", "etag-1", null, FileSyncStatus.PendingDownload, SyncDirection.None)
        };

        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("Download failed"));

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };
        using var cts = new CancellationTokenSource();
        var (completedFiles, completedBytes) = await service.ExecuteDownloadsAsync(accountId, existingItems, filesToDownload, maxParallelDownloads: 3, conflictCount: 0, totalFiles: 1,
            totalBytes: 100, uploadBytes: 0, downloadBytes: 100, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        completedFiles.ShouldBe(1);
        completedBytes.ShouldBe(100);
        await mocks.DriveItemsRepository.Received().SaveBatchAsync(Arg.Any<IEnumerable<FileMetadata>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteUploadsAsync_RespectMaxParallelLimit()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToUpload = Enumerable.Range(1, 10).Select(i => new FileMetadata(
            "", accountId, $"file{i}.txt", $"/Documents/file{i}.txt", 100, DateTime.UtcNow,
            $@"C:\Sync\Documents\file{i}.txt", false, false, false, null, null, null, $"hash{i}",
            FileSyncStatus.PendingUpload, SyncDirection.None)).ToList();

        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockObj = new object();

        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                lock(lockObj)
                {
                    currentConcurrent++;
                    maxConcurrent = Math.Max(maxConcurrent, currentConcurrent);
                }

                await Task.Delay(50, callInfo.ArgAt<CancellationToken>(4));

                lock(lockObj) currentConcurrent--;

                return new DriveItem { Id = Guid.CreateVersion7().ToString(), Name = "test.txt", CTag = "ctag", ETag = "etag", LastModifiedDateTime = DateTimeOffset.UtcNow };
            });

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };
        using var cts = new CancellationTokenSource();
        _ = await service.ExecuteUploadsAsync(accountId, existingItems, filesToUpload, maxParallelUploads: 2, conflictCount: 0, totalFiles: 10, totalBytes: 1000,
            uploadBytes: 1000, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteDownloadsAsync_RespectMaxParallelLimit()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToDownload = Enumerable.Range(1, 10).Select(i => new FileMetadata(
            $"item-{i}", accountId, $"file{i}.txt", $"/Documents/file{i}.txt", 100, DateTime.UtcNow,
            $@"C:\Sync\Documents\file{i}.txt", false, false, false, "remote-hash", "ctag", "etag", null,
            FileSyncStatus.PendingDownload, SyncDirection.None)).ToList();

        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockObj = new object();

        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                lock(lockObj)
                {
                    currentConcurrent++;
                    maxConcurrent = Math.Max(maxConcurrent, currentConcurrent);
                }

                await Task.Delay(50, callInfo.ArgAt<CancellationToken>(3));

                lock(lockObj) currentConcurrent--;
            });

        _ = mocks.LocalFileScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("hash"));

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };
        using var cts = new CancellationTokenSource();
        _ = await service.ExecuteDownloadsAsync(accountId, existingItems, filesToDownload, maxParallelDownloads: 3, conflictCount: 0, totalFiles: 10, totalBytes: 1000,
            uploadBytes: 0, downloadBytes: 1000, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        maxConcurrent.ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExecuteUploadsAsync_ReportProgressDuringUpload()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToUpload = new List<FileMetadata>
        {
            new("", accountId, "test.txt", "/Documents/test.txt", 100, DateTime.UtcNow, @"C:\Sync\Documents\test.txt",
                false, false, false, null, null, null, "hash1", FileSyncStatus.PendingUpload, SyncDirection.None)
        };

        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var progress = callInfo.ArgAt<IProgress<long>?>(3);
                progress?.Report(50);
                progress?.Report(100);
                return Task.FromResult(new DriveItem { Id = "item-123", Name = "test.txt", CTag = "ctag-1", ETag = "etag-1", LastModifiedDateTime = DateTimeOffset.UtcNow });
            });

        var progressCallCount = 0;
        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => progressCallCount++;

        using var cts = new CancellationTokenSource();
        _ = await service.ExecuteUploadsAsync(accountId, existingItems, filesToUpload, maxParallelUploads: 3, conflictCount: 0, totalFiles: 1, totalBytes: 100,
            uploadBytes: 100, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken);

        progressCallCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteUploadsAsync_HandleCancellationRequest()
    {
        var (service, mocks) = CreateTestService();
        var accountId = "test-account";
        var existingItems = Array.Empty<DriveItemEntity>().ToList().AsReadOnly();
        var filesToUpload = Enumerable.Range(1, 5).Select(i => new FileMetadata(
            "", accountId, $"file{i}.txt", $"/Documents/file{i}.txt", 100, DateTime.UtcNow,
            $@"C:\Sync\Documents\file{i}.txt", false, false, false, null, null, null, $"hash{i}",
            FileSyncStatus.PendingUpload, SyncDirection.None)).ToList();

        using var cts = new CancellationTokenSource();
        var uploadCount = 0;

        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                if(Interlocked.Increment(ref uploadCount) == 2)
                    await cts.CancelAsync();

                await Task.Delay(100, callInfo.ArgAt<CancellationToken>(4));
                return new DriveItem { Id = Guid.CreateVersion7().ToString(), Name = "test.txt", CTag = "ctag", ETag = "etag", LastModifiedDateTime = DateTimeOffset.UtcNow };
            });

        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter = (_, _, _, _, _, _, _, _, _, _, _, _) => { };

        await Should.ThrowAsync<TaskCanceledException>(async () =>
            await service.ExecuteUploadsAsync(accountId, existingItems, filesToUpload, maxParallelUploads: 3, conflictCount: 0, totalFiles: 5, totalBytes: 500,
                uploadBytes: 500, completedFiles: 0, completedBytes: 0, sessionId: null, progressReporter, cts, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphApiClientIsNull()
    {
        IGraphApiClient? graphApiClient = null;
        var localFileScanner = Substitute.For<ILocalFileScanner>();
        var driveItemsRepository = Substitute.For<IDriveItemsRepository>();
        var fileOperationLogRepository = Substitute.For<IFileOperationLogRepository>();

        Should.Throw<ArgumentNullException>(() =>
            new FileTransferService(graphApiClient!, localFileScanner, driveItemsRepository, fileOperationLogRepository));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLocalFileScannerIsNull()
    {
        var graphApiClient = Substitute.For<IGraphApiClient>();
        ILocalFileScanner? localFileScanner = null;
        var driveItemsRepository = Substitute.For<IDriveItemsRepository>();
        var fileOperationLogRepository = Substitute.For<IFileOperationLogRepository>();

        Should.Throw<ArgumentNullException>(() =>
            new FileTransferService(graphApiClient, localFileScanner!, driveItemsRepository, fileOperationLogRepository));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenDriveItemsRepositoryIsNull()
    {
        var graphApiClient = Substitute.For<IGraphApiClient>();
        var localFileScanner = Substitute.For<ILocalFileScanner>();
        IDriveItemsRepository? driveItemsRepository = null;
        var fileOperationLogRepository = Substitute.For<IFileOperationLogRepository>();

        Should.Throw<ArgumentNullException>(() =>
            new FileTransferService(graphApiClient, localFileScanner, driveItemsRepository!, fileOperationLogRepository));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFileOperationLogRepositoryIsNull()
    {
        var graphApiClient = Substitute.For<IGraphApiClient>();
        var localFileScanner = Substitute.For<ILocalFileScanner>();
        var driveItemsRepository = Substitute.For<IDriveItemsRepository>();
        IFileOperationLogRepository? fileOperationLogRepository = null;

        Should.Throw<ArgumentNullException>(() =>
            new FileTransferService(graphApiClient, localFileScanner, driveItemsRepository, fileOperationLogRepository!));
    }

    private static (FileTransferService service, TestMocks mocks) CreateTestService()
    {
        var graphApiClient = Substitute.For<IGraphApiClient>();
        var localFileScanner = Substitute.For<ILocalFileScanner>();
        var driveItemsRepository = Substitute.For<IDriveItemsRepository>();
        var fileOperationLogRepository = Substitute.For<IFileOperationLogRepository>();

        _ = driveItemsRepository.SaveBatchAsync(Arg.Any<IEnumerable<FileMetadata>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = driveItemsRepository.AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = driveItemsRepository.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FileMetadata?)null);
        _ = driveItemsRepository.GetByPathAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((FileMetadata?)null);
        _ = fileOperationLogRepository.AddAsync(Arg.Any<FileOperationLog>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new FileTransferService(graphApiClient, localFileScanner, driveItemsRepository, fileOperationLogRepository);
        var mocks = new TestMocks(graphApiClient, localFileScanner, driveItemsRepository, fileOperationLogRepository);

        return (service, mocks);
    }

    private sealed record TestMocks(
        IGraphApiClient GraphApiClient,
        ILocalFileScanner LocalFileScanner,
        IDriveItemsRepository DriveItemsRepository,
        IFileOperationLogRepository FileOperationLogRepository);
}
