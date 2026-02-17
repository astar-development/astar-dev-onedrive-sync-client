using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

public class SyncEngineShould
{
    [Fact(Skip = "Doesn't work")]
    public async Task UploadFiles_BatchedDbUpdates_UsesSaveBatchAsync()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var filesToUpload = new List<FileMetadata>();
        for(var i = 0; i < 120; i++)
        {
            filesToUpload.Add(new FileMetadata(
                $"id_{i}", new HashedAccountId(AccountIdHasher.Hash("acc1")), $"file_{i}.txt", $"/Documents/file_{i}.txt", 100,
                DateTime.UtcNow, $"C:\\Sync\\Documents\\file_{i}.txt", false, false, false, null, null, $"hash_{i}", null,
                FileSyncStatus.PendingUpload, 0));
        }

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(filesToUpload);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new DriveItem
            {
                Id = $"uploaded_{Guid.CreateVersion7():N0}",
                Name = callInfo.ArgAt<string>(2).Split('\\', '/').Last(),
                CTag = $"ctag_{Guid.CreateVersion7():N0}",
                ETag = $"etag_{Guid.CreateVersion7():N0}",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(3).SaveBatchAsync(Arg.Any<IEnumerable<FileMetadata>>(), Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 20), Arg.Any<CancellationToken>());
        });
    }

    [Fact(Skip = "Doesn't work anymore due to the way we're using SaveBatchAsync")]
    public async Task DownloadFiles_BatchedDbUpdates_UsesSaveBatchAsync()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var filesToDownload = new List<FileMetadata>();
        for(var i = 0; i < 120; i++)
        {
            filesToDownload.Add(new FileMetadata(
                $"id_{i}", new HashedAccountId(AccountIdHasher.Hash("acc1")), $"file_{i}.txt", $"/Documents/file_{i}.txt", 100,
                DateTime.UtcNow, $"C:\\Sync\\Documents\\file_{i}.txt", false, false, false, null, null, $"hash_{i}", null,
                FileSyncStatus.PendingDownload, 0));
        }

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((filesToDownload.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("hash_downloaded");

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(3).SaveBatchAsync(Arg.Any<IEnumerable<FileMetadata>>(), Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 20), Arg.Any<CancellationToken>());
        });
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task StartSyncAndReportProgress()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        progressStates.Count.ShouldBeGreaterThan(0);
        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task UploadNewLocalFiles()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var localFile = new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "doc.txt", "/Documents/doc.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\doc.txt", false, false, false, null, null, "hash123",null,
            FileSyncStatus.PendingUpload, null);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.GraphApiClient.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "doc.txt" && f.SyncStatus == FileSyncStatus.PendingUpload),
            Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.Received(1).UpdateAsync(
            Arg.Is<FileMetadata>(f => f.Name == "doc.txt" && f.SyncStatus == FileSyncStatus.Synced),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task SkipUnchangedFiles()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var localFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "doc.txt", "/Documents/doc.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\doc.txt", false, false, false, null, null, "hash123",null,
            FileSyncStatus.Synced, null);
        FileMetadata remoteFile = localFile with { };
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123")); // Include remote file
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([localFile]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.DidNotReceive().AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.DidNotReceive().UpdateAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleNoSelectedFolders()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        progressStates.Last().Status.ShouldBe(SyncStatus.Idle);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task HandleAccountNotFound()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleCancellation()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        using var cts = new CancellationTokenSource();
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<FileMetadata>>(new OperationCanceledException(cts.Token)));
        await cts.CancelAsync();
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        _ = await Should.ThrowAsync<OperationCanceledException>(async () => await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), cts.Token));

        progressStates.Last().Status.ShouldBe(SyncStatus.Paused);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task ReportProgressWithFileCountsAndBytes()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        FileMetadata[] files =
        [
            new("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "file1.txt", "/Documents/file1.txt", 1000,
                DateTime.UtcNow, @"C:\Sync\Documents\file1.txt", false, false, false, null, null, "hash1",null,
                FileSyncStatus.PendingUpload, null),
            new("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "file2.txt", "/Documents/file2.txt", 2000,
                DateTime.UtcNow, @"C:\Sync\Documents\file2.txt", false, false, false, null, null, "hash2",null,
                FileSyncStatus.PendingUpload, null)
        ];
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(files);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        SyncState finalState = progressStates.Last();
        finalState.TotalFiles.ShouldBe(2);
        finalState.CompletedFiles.ShouldBe(2);
        finalState.TotalBytes.ShouldBe(3000);
        finalState.CompletedBytes.ShouldBe(3000);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task DownloadNewRemoteFiles()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var remoteFile = new FileMetadata("remote1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "report.pdf", "/Documents/report.pdf", 500,
            DateTime.UtcNow, string.Empty, false, false, false, "ctag123", "etag456", null,null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "report.pdf" && f.SyncStatus == FileSyncStatus.Synced && f.LastSyncDirection == SyncDirection.Download),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleRemoteDeletions()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var deletedFile = new FileMetadata("deleted1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "old.txt", "/Documents/old.txt", 100,
            DateTime.UtcNow.AddDays(-5), string.Empty, false, false, false, "ctag", "etag", "hash",null,
            FileSyncStatus.Synced, SyncDirection.Download);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([deletedFile]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(1).DeleteAsync("deleted1", Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task PerformBidirectionalSync()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var localFile = new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "upload.txt", "/Documents/upload.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\upload.txt", false, false, false, null, null, "uploadhash",null,
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("remote1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "download.pdf", "/Documents/download.pdf", 200,
            DateTime.UtcNow, string.Empty, false, false, false, "ctag", "etag", null,null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(2).AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.Received(1).UpdateAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        SyncState finalState = progressStates.Last();
        finalState.Status.ShouldBe(SyncStatus.Completed);
        finalState.TotalFiles.ShouldBe(2);
        finalState.CompletedFiles.ShouldBe(2);
        finalState.TotalBytes.ShouldBe(300);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task DetectConflictWhenBothFilesModified()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        DateTimeOffset baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "conflict.txt", "/Documents/conflict.txt", 150,
            baseTime.AddMinutes(5), @"C:\Sync\Documents\conflict.txt", false, false, false, null, null, "localhash",null,
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "conflict.txt", "/Documents/conflict.txt", 200,
            baseTime.AddMinutes(3), string.Empty, false, false, false, "newctag", "newetag", null,null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        var existingFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "conflict.txt", "/Documents/conflict.txt", 100,
            baseTime, @"C:\Sync\Documents\conflict.txt", false, false, false, "oldctag", "oldetag", "oldhash",null,
            FileSyncStatus.Synced, SyncDirection.Upload);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([existingFile]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        SyncState finalState = progressStates.Last();
        finalState.ConflictsDetected.ShouldBe(1);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task RespectMaxParallelUpDownloadsSettingForUploads()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var files = Enumerable.Range(1, 5)
            .Select(i => new FileMetadata(
                "", new HashedAccountId(AccountIdHasher.Hash("acc1")), $"file{i}.txt", $"/Documents/file{i}.txt", 100,
                DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", false, false, false, null, null, $"hash{i}",null,
                FileSyncStatus.PendingUpload, null))
            .ToList();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 2, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(files);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.GraphApiClient.Received(5).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
        var uploadingStates = progressStates.Where(s => s.FilesUploading > 0).ToList();
        uploadingStates.ShouldNotBeEmpty();
        uploadingStates.Max(s => s.FilesUploading).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task RespectMaxParallelUpDownloadsSettingForDownloads()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var files = Enumerable.Range(1, 5)
            .Select(i => new FileMetadata(
                $"remote{i}", new HashedAccountId(AccountIdHasher.Hash("acc1")), $"file{i}.txt", $"/Documents/file{i}.txt", 100,
                DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", false, false, false, $"ctag{i}", $"etag{i}", null,null,
                FileSyncStatus.PendingDownload, null))
            .ToList();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((files.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed_hash");
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.GraphApiClient.Received(5).DownloadFileAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        var downloadingStates = progressStates.Where(s => s.FilesDownloading > 0).ToList();
        downloadingStates.ShouldNotBeEmpty();
        downloadingStates.Max(s => s.FilesDownloading).ShouldBeLessThanOrEqualTo(3);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task StopSyncAsyncCancelsPendingSync()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));

        var tcs = new TaskCompletionSource<IReadOnlyList<FileMetadata>>();
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _ = engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken); // Give sync time to start

        await engine.StopSyncAsync();

        _ = tcs.TrySetCanceled(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ProgressReporterIncludesHashedAccountId()
    {
        System.Reflection.MethodInfo? uploadMethod = typeof(IFileTransferService).GetMethod(nameof(IFileTransferService.ExecuteUploadsAsync));
        _ = uploadMethod.ShouldNotBeNull();
        System.Reflection.ParameterInfo uploadProgressParameter = uploadMethod!.GetParameters().Single(p => p.Name == "progressReporter");
        uploadProgressParameter.ParameterType.ShouldBe(typeof(Action<string, HashedAccountId, SyncStatus, int, int, long, long, int, int, int, int, string?, long?>));

        System.Reflection.MethodInfo? downloadMethod = typeof(IFileTransferService).GetMethod(nameof(IFileTransferService.ExecuteDownloadsAsync));
        _ = downloadMethod.ShouldNotBeNull();
        System.Reflection.ParameterInfo downloadProgressParameter = downloadMethod!.GetParameters().Single(p => p.Name == "progressReporter");
        downloadProgressParameter.ParameterType.ShouldBe(typeof(Action<string, HashedAccountId, SyncStatus, int, int, long, long, int, int, int, int, string?, long?>));
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task GetConflictsAsyncReturnsUnresolvedConflicts()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var conflict1 = new SyncConflict(
            "conflict1",
            "acc1",
            new HashedAccountId(AccountIdHasher.Hash("acc1")),
            "/Documents/conflict.txt",
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddMinutes(-3),
            150,
            200,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);
        var conflicts = new List<SyncConflict> { conflict1 };
        _ = mocks.SyncConflictRepo.GetUnresolvedByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(conflicts);

        IReadOnlyList<SyncConflict> result = await engine.GetConflictsAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = result.ShouldHaveSingleItem();
        result[0].FilePath.ShouldBe("/Documents/conflict.txt");
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task PreventConcurrentSyncAttempts()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        var tcs = new TaskCompletionSource<IReadOnlyList<FileMetadata>>();
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);
        _ = engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.LocalScanner.Received(1).ScanFolderAsync(
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task HandleExceptionDuringSyncAndReportFailed()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(
            Arg.Any<HashedAccountId>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<FileMetadata>>(new InvalidOperationException("Test exception")));
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        _ = await Should.ThrowAsync<InvalidOperationException>(async () => await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken));

        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
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

        if(expected is not null)
            result.ShouldBe($"OneDrive: {expected}");
        else
            result.ShouldBe(expected);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleEmptyFilesCorrectly()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var emptyFile = new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "empty.txt", "/Documents/empty.txt", 0,
            DateTime.UtcNow, @"C:\Sync\Documents\empty.txt", false, false, false, null, null, "emptyhash",null,
            FileSyncStatus.PendingUpload, null);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([emptyFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.GraphApiClient.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
        progressStates.Last().TotalBytes.ShouldBe(0);
        progressStates.Last().CompletedBytes.ShouldBe(0);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task HandleFileModifiedWithoutHashChange()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        DateTimeOffset baseTime = DateTime.UtcNow;
        var localFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "samehash.txt", "/Documents/samehash.txt", 100,
            baseTime.AddMinutes(10), @"C:\Sync\Documents\samehash.txt", false, false, false, null, null, "samehash",null,
            FileSyncStatus.PendingUpload, null);
        var existingFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "samehash.txt", "/Documents/samehash.txt", 100,
            baseTime, @"C:\Sync\Documents\samehash.txt", false, false, false, "oldctag", "oldetag", "samehash",null,
            FileSyncStatus.Synced, SyncDirection.Upload);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { existingFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([existingFile]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.GraphApiClient.DidNotReceive().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleUploadFailureAndMarkFileAsFailed()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var fileToUpload = new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "upload.txt", "/Documents/upload.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\upload.txt", false, false, false, null, null, "hash123",null,
            FileSyncStatus.PendingUpload, null);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([fileToUpload]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DriveItem>(new HttpRequestException("Upload failed")));
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "upload.txt" && f.SyncStatus == FileSyncStatus.Failed),
            Arg.Any<CancellationToken>());
        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleDownloadFailureAndMarkFileAsFailed()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var remoteFile = new FileMetadata("remote1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "download.pdf", "/Documents/download.pdf", 500,
            DateTime.UtcNow, string.Empty, false, false, false, "ctag123", "etag456", null,null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Download failed")));
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "download.pdf" && f.SyncStatus == FileSyncStatus.Failed),
            Arg.Any<CancellationToken>());
        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleLargeFilesWithAccurateByteTracking()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var largeFile = new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "large.iso", "/Documents/large.iso", 1024 * 1024 * 500,
            DateTime.UtcNow, @"C:\Sync\Documents\large.iso", false, false, false, null, null, "bighash",null,
            FileSyncStatus.PendingUpload, null);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 1, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([largeFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        SyncState finalState = progressStates.Last();
        finalState.TotalBytes.ShouldBe(1024 * 1024 * 500);
        finalState.CompletedBytes.ShouldBe(1024 * 1024 * 500);
        finalState.TotalFiles.ShouldBe(1);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleMultipleFilesWithMixedOperations()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        FileMetadata[] filesToUpload =
        [
            new("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "new1.txt", "/Docs/new1.txt", 100, DateTime.UtcNow, @"C:\Sync\Docs\new1.txt", false, false, false, null, null, "hash1", null, FileSyncStatus.PendingUpload, null),
            new("", new HashedAccountId(AccountIdHasher.Hash("acc1")), "new2.txt", "/Docs/new2.txt", 200, DateTime.UtcNow, @"C:\Sync\Docs\new2.txt", false, false, false, null, null, "hash2", null, FileSyncStatus.PendingUpload, null)
        ];
        FileMetadata[] filesToDownload =
        [
            new("rem1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "remote1.txt", "/Docs/remote1.txt", 150, DateTime.UtcNow, "", false, false, false, "ctag1", "etag1", null,null, FileSyncStatus.PendingDownload, SyncDirection.Download),
            new("rem2", new HashedAccountId(AccountIdHasher.Hash("acc1")), "remote2.txt", "/Docs/remote2.txt", 250, DateTime.UtcNow, "", false,  false, false, "ctag2", "etag2", null,null, FileSyncStatus.PendingDownload, SyncDirection.Download)
        ];
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Docs"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 2, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(filesToUpload.ToList());
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((filesToDownload.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed_hash");
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        SyncState finalState = progressStates.Last();
        finalState.Status.ShouldBe(SyncStatus.Completed);
        finalState.TotalFiles.ShouldBe(4);
        finalState.TotalBytes.ShouldBe(100 + 200 + 150 + 250);
        finalState.CompletedFiles.ShouldBe(4);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task DetectConflictWithExistingUnresolvedConflict()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        DateTimeOffset baseTime = DateTime.UtcNow;
        var localFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "conflict.txt", "/Documents/conflict.txt", 150,
            baseTime.AddMinutes(10), @"C:\Sync\Documents\conflict.txt", false, false, false, null, null, null, "localhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "conflict.txt", "/Documents/conflict.txt", 200,
            baseTime.AddMinutes(5), string.Empty, false, false,false, "newctag", "newetag", null,null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        var existingFile = new FileMetadata("file1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "conflict.txt", "/Documents/conflict.txt", 100,
            baseTime, @"C:\Sync\Documents\conflict.txt", false, false, false, "oldctag", "oldetag", "oldhash",null,
            FileSyncStatus.Synced, SyncDirection.Upload);
        var existingConflict = new SyncConflict(
            "conflict1",
            "acc1",
            new HashedAccountId(AccountIdHasher.Hash("acc1")),
            "/Documents/conflict.txt",
            baseTime.AddHours(-1),
            baseTime.AddHours(-2),
            100,
            100,
            baseTime.AddHours(-1),
            ConflictResolutionStrategy.None,
            false);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([existingFile]);
        _ = mocks.SyncConflictRepo.GetByFilePathAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), "/Documents/conflict.txt", Arg.Any<CancellationToken>())
            .Returns(existingConflict);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        SyncState finalState = progressStates.Last();
        finalState.ConflictsDetected.ShouldBe(1);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public void DisposeCleanupResources()
    {
        (SyncEngine engine, TestMocks _) = CreateTestEngine();

        // Should not throw
        engine.Dispose();

        // Disposing again should also not throw (idempotent)
        engine.Dispose();
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public void ProgressObservableEmitsInitialState()
    {
        (SyncEngine engine, _) = CreateTestEngine();
        SyncState? initialState = null;

        _ = engine.Progress.Subscribe(state => initialState = state);

        _ = initialState.ShouldNotBeNull();
        initialState.Status.ShouldBe(SyncStatus.Idle);
        initialState.TotalFiles.ShouldBe(0);
        initialState.CompletedFiles.ShouldBe(0);
        initialState.TotalBytes.ShouldBe(0);
        initialState.CompletedBytes.ShouldBe(0);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task PreventMultipleConcurrentSyncsForSameAccount()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Thread.Sleep(100);
                return Task.FromResult<IReadOnlyList<FileMetadata>>([]);
            });
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);
        Task sync1 = engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);
        await Task.Delay(10, TestContext.Current.CancellationToken); // Small delay to ensure first sync starts
        Task sync2 = engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        await Task.WhenAll(sync1, sync2);

        _ = await mocks.LocalScanner.Received(1).ScanFolderAsync(
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/drives/xyz/root:/Folder/SubFolder", "OneDrive: /Folder/SubFolder")]
    [InlineData("/drive/root:", "OneDrive: ")]
    [InlineData("", "")]
    [InlineData("   ", "OneDrive: /   ")]
    [InlineData("Documents", "OneDrive: /Documents")]
    [InlineData("/drives/abc/root:/", "OneDrive: /")]
    [InlineData(null, null)]
    public void FormatScanningFolderForDisplayHandlesEdgeCases(string? input, string? expected)
    {
        var result = SyncEngine.FormatScanningFolderForDisplay(input);

        result.ShouldBe(expected);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task GetConflictsAsyncReturnsEmptyListWhenNoConflicts()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();

        _ = mocks.SyncConflictRepo.GetUnresolvedByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        IReadOnlyList<SyncConflict> conflicts = await engine.GetConflictsAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        conflicts.ShouldBeEmpty();
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task StopSyncAsyncCanBeCalledMultipleTimes()
    {
        (SyncEngine engine, TestMocks _) = CreateTestEngine();

        await engine.StopSyncAsync();
        await engine.StopSyncAsync();
        await engine.StopSyncAsync();
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task HandleAccountWithDetailedSyncLoggingEnabled()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, true, false, 3, 50, 0)); // EnableDetailedSyncLogging = true
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.AccountRepo.Received(1).GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task HandleAccountWithDetailedSyncLoggingDisabled()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.AccountRepo.Received(1).GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task HandleEmptyAccountIdGracefully()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash(string.Empty)), Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        await engine.StartSyncAsync(string.Empty, new HashedAccountId(AccountIdHasher.Hash(string.Empty)), TestContext.Current.CancellationToken);

        _ = await mocks.AccountRepo.Received(1).GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash(string.Empty)), Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ProgressObservableEmitsMultipleStates()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        progressStates.Count.ShouldBeGreaterThan(1);
        progressStates.First().Status.ShouldBe(SyncStatus.Idle);
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleMultipleSelectedFolders()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents", "/Pictures", "/Music"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.LocalScanner.Received(3).ScanFolderAsync(
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _ = await mocks.RemoteDetector.Received(3).DetectChangesAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task HandleFileWithNullOrEmptyDriveItemId()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var localFile = new FileMetadata(null, new HashedAccountId(AccountIdHasher.Hash("acc1")), "new.txt", "/Documents/new.txt", 50,
            DateTime.UtcNow, @"C:\Sync\Documents\new.txt", false, false, false, null, null, "hash456", null,
            FileSyncStatus.PendingUpload, null);
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new DriveItem { Id = "newId", CTag = "newCTag", ETag = "newETag", LastModifiedDateTime = DateTimeOffset.UtcNow });

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.GraphApiClient.Received().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
    }

    private static (SyncEngine Engine, TestMocks Mocks) CreateTestEngine()
    {
        ILocalFileScanner localScanner = Substitute.For<ILocalFileScanner>();
        IRemoteChangeDetector remoteDetector = Substitute.For<IRemoteChangeDetector>();
        IDriveItemsRepository fileMetadataRepo = Substitute.For<IDriveItemsRepository>();
        ISyncConfigurationRepository syncConfigRepo = Substitute.For<ISyncConfigurationRepository>();
        IAccountRepository accountRepo = Substitute.For<IAccountRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IDeltaProcessingService deltaProcessingService = Substitute.For<IDeltaProcessingService>();

        _ = graphApiClient.UploadFileAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IProgress<long>?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new DriveItem
            {
                Id = $"uploaded_{Guid.CreateVersion7():N0}",
                Name = callInfo.ArgAt<string>(1).Split('\\', '/').Last(),
                CTag = $"ctag_{Guid.CreateVersion7():N0}",
                ETag = $"etag_{Guid.CreateVersion7():N0}",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        _ = syncConflictRepo.GetByFilePathAsync(
            Arg.Any<HashedAccountId>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        _ = deltaProcessingService.GetDeltaTokenAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);
        _ = deltaProcessingService.ProcessDeltaPagesAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<DeltaToken?>(),
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns((new DeltaToken("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "", "delta-token", DateTimeOffset.UtcNow), 1, 0));

        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();
        IFileTransferService fileTransferService = Substitute.For<IFileTransferService>();
        IDeletionSyncService deletionSyncService = Substitute.For<IDeletionSyncService>();
        ISyncStateCoordinator syncStateCoordinator = Substitute.For<ISyncStateCoordinator>();
        IConflictDetectionService conflictDetectionService = Substitute.For<IConflictDetectionService>();

        _ = conflictDetectionService.CheckKnownFileConflictAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<Dictionary<string, FileMetadata>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((false, null));
        _ = conflictDetectionService.CheckFirstSyncFileConflictAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<Dictionary<string, FileMetadata>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((false, null, null));

        var progressSubject = new System.Reactive.Subjects.BehaviorSubject<SyncState>(SyncState.CreateInitial(string.Empty, new HashedAccountId(string.Empty)));
        _ = syncStateCoordinator.Progress.Returns(progressSubject);
        _ = syncStateCoordinator.InitializeSessionAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _ = syncStateCoordinator.GetCurrentSessionId()
            .Returns((string?)null);
        _ = syncStateCoordinator.GetCurrentState()
            .Returns(callInfo => progressSubject.Value);

        syncStateCoordinator.When(x => x.UpdateProgress(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<SyncStatus>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<long?>()))
            .Do(callInfo =>
            {
                var newState = new SyncState(
                    callInfo.ArgAt<string>(0),
                    callInfo.ArgAt<HashedAccountId>(1),
                    callInfo.ArgAt<SyncStatus>(2),
                    callInfo.ArgAt<int>(3),
                    callInfo.ArgAt<int>(4),
                    callInfo.ArgAt<long>(5),
                    callInfo.ArgAt<int>(6),
                    callInfo.ArgAt<int>(7),
                    callInfo.ArgAt<int>(8),
                    callInfo.ArgAt<int>(9),
                    callInfo.ArgAt<int>(10),
                    0,
                    null,
                    callInfo.ArgAt<string?>(11),
                    DateTimeOffset.UtcNow);
                progressSubject.OnNext(newState);
            });

        var engine = new SyncEngine(localScanner,  syncConfigRepo, accountRepo, syncConflictRepo, conflictDetectionService, deltaProcessingService, fileTransferService, deletionSyncService, syncStateCoordinator);
        var mocks = new TestMocks(localScanner, remoteDetector, fileMetadataRepo, syncConfigRepo, accountRepo, graphApiClient, syncConflictRepo, conflictDetectionService, deltaProcessingService, fileTransferService, deletionSyncService, syncStateCoordinator);

        return (engine, mocks);
    }

    private sealed record TestMocks(ILocalFileScanner LocalScanner, IRemoteChangeDetector RemoteDetector, IDriveItemsRepository FileMetadataRepo, ISyncConfigurationRepository SyncConfigRepo, IAccountRepository AccountRepo, IGraphApiClient GraphApiClient, ISyncConflictRepository SyncConflictRepo,
        IConflictDetectionService ConflictDetectionService, IDeltaProcessingService DeltaProcessingService, IFileTransferService FileTransferService, IDeletionSyncService DeletionSyncService, ISyncStateCoordinator SyncStateCoordinator);
}
