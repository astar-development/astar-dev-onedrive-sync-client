using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services.OneDriveServices;
using AStarOneDriveClient.Services.Sync;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStarOneDriveClient.Tests.Unit.Services.Sync;

public sealed class ConflictResolverShould
{
    private readonly IGraphApiClient _graphApiClient = Substitute.For<IGraphApiClient>();
    private readonly IFileMetadataRepository _metadataRepo = Substitute.For<IFileMetadataRepository>();
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly ILogger<ConflictResolver> _logger = Substitute.For<ILogger<ConflictResolver>>();

    [Fact]
    public void ThrowArgumentNullExceptionWhenConflictIsNull()
    {
        var resolver = CreateResolver();

        var exception = Should.Throw<ArgumentNullException>(async () =>
            await resolver.ResolveAsync(null!, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

        exception.ParamName.ShouldBe("conflict");
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWhenAccountNotFound()
    {
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

        exception.Message.ShouldContain("Account not found");
    }

    [Fact]
    public async Task KeepLocalVersionByUploadingLocalFile()
    {
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();
        var account = CreateTestAccount();
        var metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Create temporary test file
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", CancellationToken.None);

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None);

            await _graphApiClient.Received(1).UploadFileAsync(
                account.AccountId,
                localPath,
                conflict.FilePath,
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m =>
                    m.Id == metadata.Id &&
                    m.SyncStatus == FileSyncStatus.Synced &&
                    m.LastSyncDirection == SyncDirection.Upload),
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
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();
        var account = CreateTestAccount();
        var metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        var exception = await Should.ThrowAsync<FileNotFoundException>(async () =>
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

        exception.Message.ShouldContain("Local file not found");
    }

    [Fact]
    public async Task KeepRemoteVersionByDownloadingRemoteFile()
    {
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();
        var account = CreateTestAccount();
        var metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        // Mock the download to create the file
        _graphApiClient.DownloadFileAsync(
            account.AccountId,
            metadata.Id,
            localPath,
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepRemote, CancellationToken.None);

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m =>
                    m.Id == metadata.Id &&
                    m.SyncStatus == FileSyncStatus.Synced &&
                    m.LastSyncDirection == SyncDirection.Download),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task KeepBothVersionsByRenamingLocalAndDownloadingRemote()
    {
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();
        var account = CreateTestAccount();
        var metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Create temporary test file
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", CancellationToken.None);

        // Mock the download to create the file
        _graphApiClient.DownloadFileAsync(
            account.AccountId,
            metadata.Id,
            localPath,
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepBoth, CancellationToken.None);

            // Verify original file now has remote content
            File.Exists(localPath).ShouldBeTrue();
            (await File.ReadAllTextAsync(localPath)).ShouldBe("remote content");

            // Verify conflict file exists with local content
            var directory = Path.GetDirectoryName(localPath)!;
            var conflictFiles = Directory.GetFiles(directory, "*Conflict*.txt");
            conflictFiles.Length.ShouldBe(1);
            (await File.ReadAllTextAsync(conflictFiles[0])).ShouldBe("local content");

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m =>
                    m.Id == metadata.Id &&
                    m.SyncStatus == FileSyncStatus.Synced &&
                    m.LastSyncDirection == SyncDirection.Download),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWhenMetadataNotFoundForKeepLocal()
    {
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();
        var account = CreateTestAccount();
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns((FileMetadata?)null);

        // Create temporary test file
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", CancellationToken.None);

        try
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

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
        var resolver = CreateResolver();
        var conflict = CreateTestConflict();
        var account = CreateTestAccount();

        _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);

        await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.None, CancellationToken.None);

        // Verify no API calls were made
        await _graphApiClient.DidNotReceive().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _graphApiClient.DidNotReceive().DownloadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _metadataRepo.DidNotReceive().UpdateAsync(
            Arg.Any<FileMetadata>(),
            Arg.Any<CancellationToken>());
    }

    private ConflictResolver CreateResolver() =>
        new(_graphApiClient, _metadataRepo, _accountRepo, _logger);

    private static SyncConflict CreateTestConflict() =>
        new(
            Id: "conflict-123",
            AccountId: "account-456",
            FilePath: "Documents/test.txt",
            LocalModifiedUtc: DateTime.UtcNow.AddHours(-1),
            RemoteModifiedUtc: DateTime.UtcNow,
            LocalSize: 100,
            RemoteSize: 200,
            DetectedUtc: DateTime.UtcNow,
            ResolutionStrategy: ConflictResolutionStrategy.None,
            IsResolved: false);

    private static AccountInfo CreateTestAccount() =>
        new(
            AccountId: "account-456",
            DisplayName: "Test User",
            LocalSyncPath: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            IsAuthenticated: true,
            LastSyncUtc: DateTime.UtcNow,
            DeltaToken: null);

    private static FileMetadata CreateTestMetadata(string accountId, string filePath) =>
        new(
            Id: "file-789",
            AccountId: accountId,
            Name: Path.GetFileName(filePath),
            Path: filePath,
            Size: 100,
            LastModifiedUtc: DateTime.UtcNow,
            LocalPath: filePath,
            CTag: "ctag-123",
            ETag: "etag-456",
            LocalHash: "hash-789",
            SyncStatus: FileSyncStatus.Synced,
            LastSyncDirection: SyncDirection.Download);
}
