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

    [Fact]
    public async Task StopSyncAsyncCancelsPendingSync()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        var tcs = new TaskCompletionSource<IReadOnlyList<FileMetadata>>();
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _ = engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken); // Give sync time to start

        await engine.StopSyncAsync();

        tcs.TrySetCanceled(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConflictsAsyncReturnsUnresolvedConflicts()
    {
        var (engine, mocks) = CreateTestEngine();
        var conflict1 = new SyncConflict(
            Id: "conflict1",
            AccountId: "acc1",
            FilePath: "/Documents/conflict.txt",
            LocalModifiedUtc: DateTime.UtcNow.AddMinutes(-5),
            RemoteModifiedUtc: DateTime.UtcNow.AddMinutes(-3),
            LocalSize: 150,
            RemoteSize: 200,
            DetectedUtc: DateTime.UtcNow,
            ResolutionStrategy: ConflictResolutionStrategy.None,
            IsResolved: false);

        var conflicts = new List<SyncConflict> { conflict1 };
        mocks.SyncConflictRepo.GetUnresolvedByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(conflicts);

        var result = await engine.GetConflictsAsync("acc1", TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].FilePath.ShouldBe("/Documents/conflict.txt");
    }

    [Fact]
    public async Task PreventConcurrentSyncAttempts()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        var tcs = new TaskCompletionSource<IReadOnlyList<FileMetadata>>();
        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _ = engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken); // First sync starts

        // Second sync attempt should return immediately without executing
        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Only one scan call should have been made
        await mocks.LocalScanner.Received(1).ScanFolderAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleExceptionDuringSyncAndReportFailed()
    {
        var (engine, mocks) = CreateTestEngine();
        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<FileMetadata>>(new InvalidOperationException("Test exception")));

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken));

        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    [Fact]
    public async Task HandleDuplicateDatabaseRecords()
    {
        var (engine, mocks) = CreateTestEngine();
        var duplicateFile1 = new FileMetadata("id1", "acc1", "duplicate.txt", "/Documents/duplicate.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\duplicate.txt", "ctag1", "etag1", "hash",
            FileSyncStatus.Synced, SyncDirection.Upload);
        var duplicateFile2 = new FileMetadata("id2", "acc1", "duplicate.txt", "/Documents/duplicate.txt", 150,
            DateTime.UtcNow.AddSeconds(-30), @"C:\Sync\Documents\duplicate.txt", "ctag2", "etag2", "hash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([duplicateFile1]);

        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { duplicateFile1 }.AsReadOnly(), "delta_123"));

        // Return duplicate records for same file
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([duplicateFile1, duplicateFile2]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Should complete successfully without errors
        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Theory]
    [InlineData("/drives/abc123/root:/Documents", "/Documents")]
    [InlineData("/drive/root:/MyFolder", "/MyFolder")]
    [InlineData("/Documents/Subfolder", "/Documents/Subfolder")]
    [InlineData(null, null)]
    [InlineData("/", "/")]
    public void FormatScanningFolderForDisplayRemovesGraphApiPrefixes(string? input, string? expected)
    {
        var result = SyncEngine.FormatScanningFolderForDisplay(input);

        if (expected is not null)
        {
            result.ShouldBe($"OneDrive: {expected}");
        }
        else
        {
            result.ShouldBe(expected);
        }
    }

    [Fact]
    public async Task HandleEmptyFilesCorrectly()
    {
        var (engine, mocks) = CreateTestEngine();
        var emptyFile = new FileMetadata("", "acc1", "empty.txt", "/Documents/empty.txt", 0,
            DateTime.UtcNow, @"C:\Sync\Documents\empty.txt", null, null, "emptyhash",
            FileSyncStatus.PendingUpload, null);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([emptyFile]);

        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));

        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Empty file should still be uploaded
        await mocks.GraphApiClient.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());

        progressStates.Last().TotalBytes.ShouldBe(0);
        progressStates.Last().CompletedBytes.ShouldBe(0);
    }

    [Fact]
    public async Task HandleFileModifiedWithoutHashChange()
    {
        var (engine, mocks) = CreateTestEngine();
        var baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", "acc1", "samehash.txt", "/Documents/samehash.txt", 100,
            baseTime.AddMinutes(10), @"C:\Sync\Documents\samehash.txt", null, null, "samehash",
            FileSyncStatus.PendingUpload, null);

        var existingFile = new FileMetadata("file1", "acc1", "samehash.txt", "/Documents/samehash.txt", 100,
            baseTime, @"C:\Sync\Documents\samehash.txt", "oldctag", "oldetag", "samehash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);

        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { existingFile }.AsReadOnly(), "delta_123"));

        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([existingFile]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // File has same hash, so should not be uploaded
        await mocks.GraphApiClient.DidNotReceive().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleUploadFailureAndMarkFileAsFailed()
    {
        var (engine, mocks) = CreateTestEngine();
        var fileToUpload = new FileMetadata("", "acc1", "upload.txt", "/Documents/upload.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\upload.txt", null, null, "hash123",
            FileSyncStatus.PendingUpload, null);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([fileToUpload]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock upload to throw exception
        mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Microsoft.Graph.Models.DriveItem>(new HttpRequestException("Upload failed")));

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Verify file was marked as Failed in database
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "upload.txt" && f.SyncStatus == FileSyncStatus.Failed),
            Arg.Any<CancellationToken>());

        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact]
    public async Task HandleDownloadFailureAndMarkFileAsFailed()
    {
        var (engine, mocks) = CreateTestEngine();
        var remoteFile = new FileMetadata("remote1", "acc1", "download.pdf", "/Documents/download.pdf", 500,
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

        // Mock download to throw exception
        mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Download failed")));

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Verify file was marked as Failed in database
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "download.pdf" && f.SyncStatus == FileSyncStatus.Failed),
            Arg.Any<CancellationToken>());

        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact]
    public async Task HandleLargeFilesWithAccurateByteTracking()
    {
        var (engine, mocks) = CreateTestEngine();
        var largeFile = new FileMetadata("", "acc1", "large.iso", "/Documents/large.iso", 1024 * 1024 * 500,
            DateTime.UtcNow, @"C:\Sync\Documents\large.iso", null, null, "bighash",
            FileSyncStatus.PendingUpload, null);

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 1, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([largeFile]);
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        var finalState = progressStates.Last();
        finalState.TotalBytes.ShouldBe(1024 * 1024 * 500);
        finalState.CompletedBytes.ShouldBe(1024 * 1024 * 500);
        finalState.TotalFiles.ShouldBe(1);
    }

    [Fact]
    public async Task HandleMultipleFilesWithMixedOperations()
    {
        var (engine, mocks) = CreateTestEngine();

        var filesToUpload = new[]
        {
            new FileMetadata("", "acc1", "new1.txt", "/Docs/new1.txt", 100, DateTime.UtcNow, @"C:\Sync\Docs\new1.txt", null, null, "hash1", FileSyncStatus.PendingUpload, null),
            new FileMetadata("", "acc1", "new2.txt", "/Docs/new2.txt", 200, DateTime.UtcNow, @"C:\Sync\Docs\new2.txt", null, null, "hash2", FileSyncStatus.PendingUpload, null),
        };

        var filesToDownload = new[]
        {
            new FileMetadata("rem1", "acc1", "remote1.txt", "/Docs/remote1.txt", 150, DateTime.UtcNow, "", "ctag1", "etag1", null, FileSyncStatus.PendingDownload, SyncDirection.Download),
            new FileMetadata("rem2", "acc1", "remote2.txt", "/Docs/remote2.txt", 250, DateTime.UtcNow, "", "ctag2", "etag2", null, FileSyncStatus.PendingDownload, SyncDirection.Download),
        };

        mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Docs"]);
        mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 2, 50, null));

        mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(filesToUpload.ToList());
        mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((filesToDownload.AsReadOnly(), "delta_123"));
        mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed_hash");

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        var finalState = progressStates.Last();
        finalState.Status.ShouldBe(SyncStatus.Completed);
        finalState.TotalFiles.ShouldBe(4);
        finalState.TotalBytes.ShouldBe(100 + 200 + 150 + 250);
        finalState.CompletedFiles.ShouldBe(4);
    }

    [Fact]
    public async Task DetectConflictWithExistingUnresolvedConflict()
    {
        var (engine, mocks) = CreateTestEngine();
        var baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 150,
            baseTime.AddMinutes(10), @"C:\Sync\Documents\conflict.txt", null, null, "localhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 200,
            baseTime.AddMinutes(5), string.Empty, "newctag", "newetag", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        var existingFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 100,
            baseTime, @"C:\Sync\Documents\conflict.txt", "oldctag", "oldetag", "oldhash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        var existingConflict = new SyncConflict(
            Id: "conflict1",
            AccountId: "acc1",
            FilePath: "/Documents/conflict.txt",
            LocalModifiedUtc: baseTime.AddHours(-1),
            RemoteModifiedUtc: baseTime.AddHours(-2),
            LocalSize: 100,
            RemoteSize: 100,
            DetectedUtc: baseTime.AddHours(-1),
            ResolutionStrategy: ConflictResolutionStrategy.None,
            IsResolved: false);

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

        mocks.SyncConflictRepo.GetByFilePathAsync("acc1", "/Documents/conflict.txt", Arg.Any<CancellationToken>())
            .Returns(existingConflict);

        var progressStates = new List<SyncState>();
        engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        var finalState = progressStates.Last();
        finalState.ConflictsDetected.ShouldBe(1);
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
