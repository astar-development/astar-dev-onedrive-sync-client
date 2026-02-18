using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Models;

public class FileOperationLogShould
{
    [Fact]
    public void CreateSyncConflictLogCorrectly()
    {
        var accountId = "account-id";
        var sessionId = Guid.CreateVersion7();
        var filePath = "/path/to/file.txt";
        var localPath = "/onedrive/path/to/file.txt";
        var oneDriveId = "onedrive-item-id";
        FileOperation operationType = FileOperation.ConflictDetected;
        var localHash = "local-file-hash";
        var fileSize = 2048L;
        DateTimeOffset lastModifiedUtc = DateTime.UtcNow.AddSeconds(-30);
        DateTimeOffset remoteLastModifiedUtc = DateTime.UtcNow;
        var reason = $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteLastModifiedUtc:yyyy-MM-dd HH:mm:ss}";

        FileOperationLog fileOperationLog = FileOperationLog.CreateSyncConflictLog(
            sessionId,
            new HashedAccountId(accountId),
            filePath,
            localPath,
            oneDriveId,
            localHash,
            fileSize,
            lastModifiedUtc,
            remoteLastModifiedUtc);

        _ = fileOperationLog.Id.ShouldNotBeNull();
        fileOperationLog.HashedAccountId.Value.ShouldBe(accountId);
        fileOperationLog.SyncSessionId.ShouldBe(sessionId);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.Operation.ShouldBe(operationType);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.LocalHash.ShouldBe(localHash);
        fileOperationLog.LocalPath.ShouldBe(localPath);
        fileOperationLog.FileSize.ShouldBe(fileSize);
        fileOperationLog.LastModifiedUtc.ShouldBe(lastModifiedUtc);
        fileOperationLog.Reason.ShouldBe(reason);
    }

    [Fact]
    public void CreateDownloadLogCorrectly()
    {
        var accountId = "account-id";
        var sessionId = Guid.CreateVersion7();
        var filePath = "/path/to/file.txt";
        var localPath = "/onedrive/path/to/file.txt";
        var oneDriveId = "onedrive-item-id";
        FileOperation operationType = FileOperation.Download;
        var localHash = "local-file-hash";
        var fileSize = 2048L;
        DateTimeOffset lastModifiedUtc = DateTime.UtcNow.AddSeconds(-30);
        DateTimeOffset remoteLastModifiedUtc = DateTime.UtcNow;
        var reason = $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteLastModifiedUtc:yyyy-MM-dd HH:mm:ss}";

        FileOperationLog fileOperationLog = FileOperationLog.CreateDownloadLog(
            sessionId,
            new HashedAccountId(accountId),
            filePath,
            localPath,
            oneDriveId,
            localHash,
            fileSize,
            lastModifiedUtc,
            reason);

        _ = fileOperationLog.Id.ShouldNotBeNull();
        fileOperationLog.HashedAccountId.Value.ShouldBe(accountId);
        fileOperationLog.SyncSessionId.ShouldBe(sessionId);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.Operation.ShouldBe(operationType);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.FileSize.ShouldBe(fileSize);
        fileOperationLog.LastModifiedUtc.ShouldBe(lastModifiedUtc);
        fileOperationLog.Reason.ShouldBe(reason);
    }

    [Fact]
    public void CreateUploadLogCorrectly()
    {
        var accountId = "account-id";
        var sessionId = Guid.CreateVersion7();
        var filePath = "/path/to/file.txt";
        var localPath = "/onedrive/path/to/file.txt";
        var oneDriveId = "onedrive-item-id";
        FileOperation operationType = FileOperation.Upload;
        var localHash = "local-file-hash";
        var fileSize = 2048L;
        DateTimeOffset lastModifiedUtc = DateTime.UtcNow.AddSeconds(-30);
        DateTimeOffset remoteLastModifiedUtc = DateTime.UtcNow;
        var reason = $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteLastModifiedUtc:yyyy-MM-dd HH:mm:ss}";

        FileOperationLog fileOperationLog = FileOperationLog.CreateUploadLog(
            sessionId,
            new HashedAccountId(accountId),
            filePath,
            localPath,
            oneDriveId,
            localHash,
            fileSize,
            lastModifiedUtc,
            reason);

        _ = fileOperationLog.Id.ShouldNotBeNull();
        fileOperationLog.HashedAccountId.Value.ShouldBe(accountId);
        fileOperationLog.SyncSessionId.ShouldBe(sessionId);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.Operation.ShouldBe(operationType);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.FileSize.ShouldBe(fileSize);
        fileOperationLog.LastModifiedUtc.ShouldBe(lastModifiedUtc);
        fileOperationLog.Reason.ShouldBe(reason);
    }
}
