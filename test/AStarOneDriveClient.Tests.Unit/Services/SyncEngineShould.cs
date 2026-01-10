using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Services.OneDriveServices;

namespace AStarOneDriveClient.Tests.Unit.Services;

public class SyncEngineShould
{
    [Fact]
    public async Task StartSyncAndReportProgress()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        progressStates.Count.ShouldBeGreaterThan(0);
        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact]
    public async Task UploadNewLocalFiles()
    {
        var (engine, mocks) = CreateTestEngine();
        var localFile = new FileMetadata("", "acc1", "doc.txt", "/Documents/doc.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\doc.txt", null, null, "hash123",
            FileSyncStatus.PendingUpload, null);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Verify UploadFileAsync was called
        await mocks.GraphApiClient.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());

        // Verify AddAsync called with PendingUpload before upload
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "doc.txt" && f.SyncStatus == FileSyncStatus.PendingUpload),
            Arg.Any<CancellationToken>());

        // Verify UpdateAsync called with Synced after upload
        await mocks.FileMetadataRepo.Received(1).UpdateAsync(
            Arg.Is<FileMetadata>(f => f.Name == "doc.txt" && f.SyncStatus == FileSyncStatus.Synced),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipUnchangedFiles()
    {
        var (engine, mocks) = CreateTestEngine();
        var localFile = new FileMetadata("file1", "acc1", "doc.txt", "/Documents/doc.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\doc.txt", null, null, "hash123",
            FileSyncStatus.Synced, null);

        // Remote file with same metadata (unchanged)
        var remoteFile = localFile with { };

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123")); // Include remote file
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([localFile]);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.DidNotReceive().AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.DidNotReceive().UpdateAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleNoSelectedFolders()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        progressStates.Last().Status.ShouldBe(SyncStatus.Idle);
    }

    [Fact]
    public async Task HandleAccountNotFound()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    [Fact]
    public async Task HandleCancellation()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        using var cts = new CancellationTokenSource();
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<FileMetadata>>(new OperationCanceledException(cts.Token)));
        await cts.CancelAsync();

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await engine.StartSyncAsync("acc1", cts.Token));

        progressStates.Last().Status.ShouldBe(SyncStatus.Paused);
    }

    [Fact]
    public async Task ReportProgressWithFileCountsAndBytes()
    {
        var (engine, mocks) = CreateTestEngine();
        var files = new[]
        {
            new FileMetadata("", "acc1", "file1.txt", "/Documents/file1.txt", 1000,
                DateTime.UtcNow, @"C:\Sync\Documents\file1.txt", null, null, "hash1",
                FileSyncStatus.PendingUpload, null),
            new FileMetadata("", "acc1", "file2.txt", "/Documents/file2.txt", 2000,
                DateTime.UtcNow, @"C:\Sync\Documents\file2.txt", null, null, "hash2",
                FileSyncStatus.PendingUpload, null)
        };

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(files);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        var finalState = progressStates.Last();
        finalState.TotalFiles.ShouldBe(2);
        finalState.CompletedFiles.ShouldBe(2);
        finalState.TotalBytes.ShouldBe(3000);
        finalState.CompletedBytes.ShouldBe(3000);
    }

    [Fact]
    public async Task DownloadNewRemoteFiles()
    {
        var (engine, mocks) = CreateTestEngine();
        var remoteFile = new FileMetadata("remote1", "acc1", "report.pdf", "/Documents/report.pdf", 500,
            DateTime.UtcNow, string.Empty, "ctag123", "etag456", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "report.pdf" && f.SyncStatus == FileSyncStatus.Synced && f.LastSyncDirection == SyncDirection.Download),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRemoteDeletions()
    {
        var (engine, mocks) = CreateTestEngine();
        var deletedFile = new FileMetadata("deleted1", "acc1", "old.txt", "/Documents/old.txt", 100,
            DateTime.UtcNow.AddDays(-5), string.Empty, "ctag", "etag", "hash",
            FileSyncStatus.Synced, SyncDirection.Download);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([deletedFile]);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(1).DeleteAsync("deleted1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PerformBidirectionalSync()
    {
        var (engine, mocks) = CreateTestEngine();
        var localFile = new FileMetadata("", "acc1", "upload.txt", "/Documents/upload.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\upload.txt", null, null, "uploadhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("remote1", "acc1", "download.pdf", "/Documents/download.pdf", 200,
            DateTime.UtcNow, string.Empty, "ctag", "etag", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Expect 2 AddAsync: PendingUpload for upload, Synced for download
        await mocks.FileMetadataRepo.Received(2).AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        // Expect 1 UpdateAsync: Synced for upload completion
        await mocks.FileMetadataRepo.Received(1).UpdateAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());

        var finalState = progressStates.Last();
        finalState.Status.ShouldBe(SyncStatus.Completed);
        finalState.TotalFiles.ShouldBe(2);
        finalState.CompletedFiles.ShouldBe(2);
        finalState.TotalBytes.ShouldBe(300);
    }

    [Fact]
    public async Task DetectConflictWhenBothFilesModified()
    {
        var (engine, mocks) = CreateTestEngine();
        var baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 150,
            baseTime.AddMinutes(5), @"C:\Sync\Documents\conflict.txt", null, null, "localhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 200,
            baseTime.AddMinutes(3), string.Empty, "newctag", "newetag", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        var existingFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 100,
            baseTime, @"C:\Sync\Documents\conflict.txt", "oldctag", "oldetag", "oldhash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([existingFile]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        var finalState = progressStates.Last();
        finalState.ConflictsDetected.ShouldBe(1);
    }

    [Fact]
    public async Task RespectMaxParallelUpDownloadsSettingForUploads()
    {
        var (engine, mocks) = CreateTestEngine();

        // Create multiple files to upload
        var files = Enumerable.Range(1, 5)
            .Select(i => new FileMetadata(
                "", "acc1", $"file{i}.txt", $"/Documents/file{i}.txt", 100,
                DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", null, null, $"hash{i}",
                FileSyncStatus.PendingUpload, null))
            .ToList();

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);

        // Set MaxParallelUpDownloads to 2
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 2, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(files);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Verify all files were uploaded
        await mocks.GraphApiClient.Received(5).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());

        // Check that progress reported uploads (filesUploading should have been > 0 at some point)
        var uploadingStates = progressStates.Where(s => s.FilesUploading > 0).ToList();
        uploadingStates.ShouldNotBeEmpty();

        // The max concurrent uploads should not exceed the configured limit
        // (Though in unit tests with mocks, this happens so fast we might not catch it,
        // but the implementation should honor the semaphore limit)
        uploadingStates.Max(s => s.FilesUploading).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task RespectMaxParallelUpDownloadsSettingForDownloads()
    {
        var (engine, mocks) = CreateTestEngine();

        // Create multiple files to download
        var files = Enumerable.Range(1, 5)
            .Select(i => new FileMetadata(
                $"remote{i}", "acc1", $"file{i}.txt", $"/Documents/file{i}.txt", 100,
                DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", $"ctag{i}", $"etag{i}", null,
                FileSyncStatus.PendingDownload, null))
            .ToList();

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);

        // Set MaxParallelUpDownloads to 3
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((files.AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock hash computation
        mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed_hash");

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Verify all files were downloaded
        await mocks.GraphApiClient.Received(5).DownloadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Check that progress reported downloads (filesDownloading should have been > 0 at some point)
        var downloadingStates = progressStates.Where(s => s.FilesDownloading > 0).ToList();
        downloadingStates.ShouldNotBeEmpty();

        // The max concurrent downloads should not exceed the configured limit
        downloadingStates.Max(s => s.FilesDownloading).ShouldBeLessThanOrEqualTo(3);
    }

    private static (SyncEngine Engine, TestMocks Mocks) CreateTestEngine()
    {
        var localScanner = Substitute.For<ILocalFileScanner>();
        var remoteDetector = Substitute.For<IRemoteChangeDetector>();
        var fileMetadataRepo = Substitute.For<IFileMetadataRepository>();
        var syncConfigRepo = Substitute.For<ISyncConfigurationRepository>();
        var accountRepo = Substitute.For<IAccountRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        var syncConflictRepo = Substitute.For<ISyncConflictRepository>();

        // Setup default mock return for UploadFileAsync to prevent null reference exceptions
        graphApiClient.UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new Microsoft.Graph.Models.DriveItem
            {
                Id = $"uploaded_{Guid.NewGuid():N}",
                Name = callInfo.ArgAt<string>(1).Split('\\', '/').Last(),
                CTag = $"ctag_{Guid.NewGuid():N}",
                ETag = $"etag_{Guid.NewGuid():N}",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        // Setup default mock for GetByFilePathAsync to return null (no existing conflict)
        syncConflictRepo.GetByFilePathAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        var syncSessionLogRepo = Substitute.For<ISyncSessionLogRepository>();
        var fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();

        var engine = new SyncEngine(localScanner, remoteDetector, fileMetadataRepo, syncConfigRepo, accountRepo, graphApiClient, syncConflictRepo, syncSessionLogRepo, fileOperationLogRepo);
        var mocks = new TestMocks(localScanner, remoteDetector, fileMetadataRepo, syncConfigRepo, accountRepo, graphApiClient, syncConflictRepo);

        return (engine, mocks);
    }

    private sealed record TestMocks(
        ILocalFileScanner LocalScanner,
        IRemoteChangeDetector RemoteDetector,
        IFileMetadataRepository FileMetadataRepo,
        ISyncConfigurationRepository SyncConfigRepo,
        IAccountRepository AccountRepo,
        IGraphApiClient GraphApiClient,
        ISyncConflictRepository SyncConflictRepo);
}
