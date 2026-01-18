using AStarOneDriveClient.Models.Enums;

namespace AStarOneDriveClient.Models;

public class SyncSessionLogShould
{
    [Fact]
    public void CreateTheInitialInstanceCorrectly()
    {
        var syncSessionLog = SyncSessionLog.CreateInitialRunning("test-account-id");

        _ = syncSessionLog.Id.ShouldNotBeNull();
        syncSessionLog.AccountId.ShouldBe("test-account-id");
        syncSessionLog.StartedUtc.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        syncSessionLog.CompletedUtc.ShouldBeNull();
        syncSessionLog.Status.ShouldBe(SyncStatus.Running);
        syncSessionLog.FilesUploaded.ShouldBe(0);
        syncSessionLog.FilesDownloaded.ShouldBe(0);
        syncSessionLog.FilesDeleted.ShouldBe(0);
        syncSessionLog.ConflictsDetected.ShouldBe(0);
        syncSessionLog.TotalBytes.ShouldBe(0L);
    }
}
