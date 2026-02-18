using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data.Entities;

public class FileOperationLogEntityShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        var fileOperationLogEntity = new FileOperationLogEntity
        {
            Id = "1",
            HashedAccountId = new HashedAccountId("hashed-account-id"),
            Timestamp = currentTime,
            Operation = 2,
            FilePath = "/local/path/to/file.txt",
            LocalPath = "/remote/path/to/file.txt",
            OneDriveId = "onedrive-id",
            FileSize = 2048,
            LocalHash = "local-hash-value",
            RemoteHash = "remote-hash-value",
            LastModifiedUtc = currentTime,
            Reason = "Test reason"
        };

        fileOperationLogEntity.Id.ShouldBe("1");
        fileOperationLogEntity.HashedAccountId.Value.ShouldBe("hashed-account-id");
        fileOperationLogEntity.Timestamp.ShouldBe(currentTime);
        fileOperationLogEntity.Operation.ShouldBe(2);
        fileOperationLogEntity.FilePath.ShouldBe("/local/path/to/file.txt");
        fileOperationLogEntity.LocalPath.ShouldBe("/remote/path/to/file.txt");
        fileOperationLogEntity.OneDriveId.ShouldBe("onedrive-id");
        fileOperationLogEntity.FileSize.ShouldBe(2048);
        fileOperationLogEntity.LocalHash.ShouldBe("local-hash-value");
        fileOperationLogEntity.RemoteHash.ShouldBe("remote-hash-value");
        fileOperationLogEntity.LastModifiedUtc.ShouldBe(currentTime);
        fileOperationLogEntity.Reason.ShouldBe("Test reason");
    }
}
