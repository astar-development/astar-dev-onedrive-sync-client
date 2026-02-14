using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;

using AStar.Dev.OneDrive.Client.SyncronisationConflicts;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.SyncronisationConflicts;

public sealed class ConflictResolverShould
{
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly ISyncConflictRepository _conflictRepo = Substitute.For<ISyncConflictRepository>();
    private readonly IGraphApiClient _graphApiClient = Substitute.For<IGraphApiClient>();
    private readonly ILocalFileScanner _localFileScanner = Substitute.For<ILocalFileScanner>();
    private readonly ILogger<ConflictResolver> _logger = Substitute.For<ILogger<ConflictResolver>>();
    private readonly IDriveItemsRepository _metadataRepo = Substitute.For<IDriveItemsRepository>();

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
        FileMetadata metadata = CreateTestMetadata(account.HashedAccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.HashedAccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", TestContext.Current.CancellationToken);

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, TestContext.Current.CancellationToken);

            _ = await _graphApiClient.Received(1).UploadFileAsync(
                account.HashedAccountId,
                localPath,
                conflict.FilePath,
                Arg.Any<IProgress<long>?>(),
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m => m.DriveItemId == metadata.DriveItemId &&
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
        FileMetadata metadata = CreateTestMetadata(account.HashedAccountId, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.HashedAccountId, conflict.FilePath, Arg.Any<CancellationToken>())
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
        FileMetadata metadata = CreateTestMetadata(account.HashedAccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);
        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.HashedAccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        _ = _graphApiClient.DownloadFileAsync(
                account.HashedAccountId,
                metadata.DriveItemId,
                localPath,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepRemote, TestContext.Current.CancellationToken);

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.HashedAccountId,
                metadata.DriveItemId,
                localPath,
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m => m.DriveItemId == metadata.DriveItemId &&
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
            if(File.Exists(localPath))
                File.Delete(localPath);

            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task KeepBothVersionsByRenamingLocalAndDownloadingRemote()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.HashedAccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);
        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.HashedAccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", TestContext.Current.CancellationToken);
        _ = _graphApiClient.DownloadFileAsync(
                account.HashedAccountId,
                metadata.DriveItemId,
                localPath,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));
        var remoteItem = new DriveItem
        {
            Id = metadata.DriveItemId,
            Name = "test.txt",
            Size = 14, // "remote content" length
            LastModifiedDateTime = DateTime.UtcNow,
            CTag = "remote-ctag",
            ETag = "remote-etag"
        };
        _ = _graphApiClient.GetDriveItemAsync(account.HashedAccountId, metadata.DriveItemId, Arg.Any<CancellationToken>())
            .Returns(remoteItem);

        _ = _localFileScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed-hash-123");

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepBoth, TestContext.Current.CancellationToken);

            File.Exists(localPath).ShouldBeTrue();
            (await File.ReadAllTextAsync(localPath, TestContext.Current.CancellationToken)).ShouldBe("remote content");

            var directory = Path.GetDirectoryName(localPath)!;
            var conflictFiles = Directory.GetFiles(directory, "*Conflict*.txt");
            conflictFiles.Length.ShouldBe(1);
            (await File.ReadAllTextAsync(conflictFiles[0], TestContext.Current.CancellationToken)).ShouldBe("local content");

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.HashedAccountId,
                metadata.DriveItemId,
                localPath,
                Arg.Any<CancellationToken>());

            _ = await _graphApiClient.Received(1).GetDriveItemAsync(
                account.HashedAccountId,
                metadata.DriveItemId,
                Arg.Any<CancellationToken>());

            _ = await _localFileScanner.Received(1).ComputeFileHashAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m => m.DriveItemId == metadata.DriveItemId &&
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
        _ = _metadataRepo.GetByPathAsync(account.HashedAccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns((FileMetadata?)null);

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

    private ConflictResolver CreateResolver() => new(_graphApiClient, _metadataRepo, _accountRepo, _conflictRepo, _localFileScanner, _logger);

    private static SyncConflict CreateTestConflict() => new(
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

    private static AccountInfo CreateTestAccount() => new(
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
                0);

    private static FileMetadata CreateTestMetadata(string accountId, string filePath) => new(
                "file-789",
                accountId,
                Path.GetFileName(filePath),
                filePath,
                100,
                DateTime.UtcNow,
                filePath, false, false, false,
                "ctag-123",
                "etag-456",
                "hash-789", null,
                FileSyncStatus.Synced,
                SyncDirection.Download);
}
