using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.ConfigurationSettings;

public class SyncSessionLogEntityShould
{
    [Fact]
    public void CreateSyncSessionLogEntityWithRequiredProperties()
    {
        var sessionId = Guid.NewGuid();
        var logEntity = new SyncSessionLogEntity
        {
            Id = sessionId,
            HashedAccountId = new HashedAccountId("hashed-account-id"),
            StartedUtc = DateTimeOffset.UtcNow,
            Status = 1,
            FilesUploaded = 10,
            FilesDownloaded = 5,
            FilesDeleted = 2,
            ConflictsDetected = 1,
            TotalBytes = 1024
        };

        logEntity.Id.ShouldBe(sessionId);
        logEntity.HashedAccountId.Value.ShouldBe("hashed-account-id");
        logEntity.StartedUtc.ShouldNotBe(default);
        logEntity.Status.ShouldBe(1);
        logEntity.FilesUploaded.ShouldBe(10);
        logEntity.FilesDownloaded.ShouldBe(5);
        logEntity.FilesDeleted.ShouldBe(2);
        logEntity.ConflictsDetected.ShouldBe(1);
        logEntity.TotalBytes.ShouldBe(1024);
    }
}
