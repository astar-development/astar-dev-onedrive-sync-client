using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Services.Sync;

public sealed class ConflictResolverShould
{
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly ISyncConflictRepository _conflictRepo = Substitute.For<ISyncConflictRepository>();
    private readonly IGraphApiClient _graphApiClient = Substitute.For<IGraphApiClient>();
    private readonly ILocalFileScanner _localFileScanner = Substitute.For<ILocalFileScanner>();
    private readonly ILogger<ConflictResolver> _logger = Substitute.For<ILogger<ConflictResolver>>();
    private readonly IFileMetadataRepository _metadataRepo = Substitute.For<IFileMetadataRepository>();

    [Fact]
    public void ThrowArgumentNullExceptionWhenConflictIsNull()
    {
        ConflictResolver resolver = CreateResolver();

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(async () =>
            await resolver.ResolveAsync(null!, ConflictResolutionStrategy.KeepLocal, TestContext.Current.CancellationToken));

        exception.ParamName.ShouldBe("conflict");
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWhenAccountNotFound()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () => await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal,
            TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("Account not found");
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task KeepLocalVersionByUploadingLocalFile()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Create temporary test file
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", TestContext.Current.CancellationToken);

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, TestContext.Current.CancellationToken);

            _ = await _graphApiClient.Received(1).UploadFileAsync(
                account.AccountId,
                localPath,
                conflict.FilePath,
                Arg.Any<IProgress<long>?>(),
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m => m.Id == metadata.Id &&
                                          m.SyncStatus == FileSyncStatus.Synced &&
                                          m.LastSyncDirection == SyncDirection.Upload),
                Arg.Any<CancellationToken>());

            await _conflictRepo.Received(1).UpdateAsync(
                Arg.Is<SyncConflict>(c => c.Id == conflict.Id &&
                                          c.IsResolved &&
                                          c.ResolutionStrategy == ConflictResolutionStrategy.KeepLocal),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(localPath);
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task ThrowFileNotFoundExceptionWhenKeepLocalAndFileDoesNotExist()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        FileNotFoundException exception = await Should.ThrowAsync<FileNotFoundException>(async () => await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal,
            TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("Local file not found");
    }

    [Fact]
    public async Task KeepRemoteVersionByDownloadingRemoteFile()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Ensure directory exists
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        // Mock the download to create the file
        _ = _graphApiClient.DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepRemote, TestContext.Current.CancellationToken);

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m => m.Id == metadata.Id &&
                                          m.SyncStatus == FileSyncStatus.Synced &&
                                          m.LastSyncDirection == SyncDirection.Download),
                Arg.Any<CancellationToken>());

            await _conflictRepo.Received(1).UpdateAsync(
                Arg.Is<SyncConflict>(c => c.Id == conflict.Id &&
                                          c.IsResolved &&
                                          c.ResolutionStrategy == ConflictResolutionStrategy.KeepRemote),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if(File.Exists(localPath)) File.Delete(localPath);

            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task KeepBothVersionsByRenamingLocalAndDownloadingRemote()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Create temporary test file
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", TestContext.Current.CancellationToken);

        // Mock the download to create the file
        _ = _graphApiClient.DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        // Mock GetDriveItemAsync to return remote file metadata
        var remoteItem = new DriveItem
        {
            Id = metadata.Id,
            Name = "test.txt",
            Size = 14, // "remote content" length
            LastModifiedDateTime = DateTime.UtcNow,
            CTag = "remote-ctag",
            ETag = "remote-etag"
        };
        _ = _graphApiClient.GetDriveItemAsync(account.AccountId, metadata.Id, Arg.Any<CancellationToken>())
            .Returns(remoteItem);

        // Mock ComputeFileHashAsync for both downloaded and conflict files
        _ = _localFileScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed-hash-123");

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepBoth, TestContext.Current.CancellationToken);

            // Verify original file now has remote content
            File.Exists(localPath).ShouldBeTrue();
            (await File.ReadAllTextAsync(localPath, TestContext.Current.CancellationToken)).ShouldBe("remote content");

            // Verify conflict file exists with local content
            var directory = Path.GetDirectoryName(localPath)!;
            var conflictFiles = Directory.GetFiles(directory, "*Conflict*.txt");
            conflictFiles.Length.ShouldBe(1);
            (await File.ReadAllTextAsync(conflictFiles[0], TestContext.Current.CancellationToken)).ShouldBe("local content");

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>());

            _ = await _graphApiClient.Received(1).GetDriveItemAsync(
                account.AccountId,
                metadata.Id,
                Arg.Any<CancellationToken>());

            _ = await _localFileScanner.Received(1).ComputeFileHashAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m => m.Id == metadata.Id &&
                                          m.SyncStatus == FileSyncStatus.Synced &&
                                          m.LastSyncDirection == SyncDirection.Download &&
                                          m.LocalHash == "computed-hash-123"),
                Arg.Any<CancellationToken>());

            await _conflictRepo.Received(1).UpdateAsync(
                Arg.Is<SyncConflict>(c => c.Id == conflict.Id &&
                                          c.IsResolved &&
                                          c.ResolutionStrategy == ConflictResolutionStrategy.KeepBoth),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task ThrowInvalidOperationExceptionWhenMetadataNotFoundForKeepLocal()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns((FileMetadata?)null);

        // Create temporary test file
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", TestContext.Current.CancellationToken);

        try
        {
            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () => await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal,
                TestContext.Current.CancellationToken));

            exception.Message.ShouldContain("File metadata not found");
        }
        finally
        {
            File.Delete(localPath);
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task SkipResolutionWhenStrategyIsNone()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);

        await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.None, TestContext.Current.CancellationToken);

        // Verify no API calls were made
        _ = await _graphApiClient.DidNotReceive().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
        await _graphApiClient.DidNotReceive().DownloadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _metadataRepo.DidNotReceive().UpdateAsync(
            Arg.Any<FileMetadata>(),
            Arg.Any<CancellationToken>());
        await _conflictRepo.DidNotReceive().UpdateAsync(
            Arg.Any<SyncConflict>(),
            Arg.Any<CancellationToken>());
    }

    private ConflictResolver CreateResolver()
        => new(_graphApiClient, _metadataRepo, _accountRepo, _conflictRepo, _localFileScanner, _logger);

    private static SyncConflict CreateTestConflict()
        => new(
            "conflict-123",
            "account-456",
            "Documents/test.txt",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            100,
            200,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);

    private static AccountInfo CreateTestAccount()
        => new(
            "account-456",
            "Test User",
            Path.Combine(Path.GetTempPath(), Guid.CreateVersion7().ToString()),
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

    private static FileMetadata CreateTestMetadata(string accountId, string filePath)
        => new(
            "file-789",
            accountId,
            Path.GetFileName(filePath),
            filePath,
            100,
            DateTime.UtcNow,
            filePath,
            "ctag-123",
            "etag-456",
            "hash-789",
            FileSyncStatus.Synced,
            SyncDirection.Download);
}
