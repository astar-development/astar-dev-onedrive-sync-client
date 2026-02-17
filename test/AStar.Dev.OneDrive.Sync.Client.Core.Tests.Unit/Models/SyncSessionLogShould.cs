using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Models;

public class SyncSessionLogShould
{
    [Fact]
    public void CreateTheInitialInstanceCorrectly()
    {
        var syncSessionLog = SyncSessionLog.CreateInitialRunning("test-account-id", AccountIdHasher.Hash("test-account-id"));

        _ = syncSessionLog.Id.ShouldNotBeNull();
        syncSessionLog.HashedAccountId.Id.ShouldBe(AccountIdHasher.Hash("test-account-id"));
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
