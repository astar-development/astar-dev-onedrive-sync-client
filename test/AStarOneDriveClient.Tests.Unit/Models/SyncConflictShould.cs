namespace AStarOneDriveClient.Models;

public class SyncConflictShould
{
    [Fact]
    public void CreateUnresolvedInstanceCorrectly()
    {
        var accountId = "account-id";
        var filePath = "/path/to/conflicted/file.txt";
        var localModifiedUtc = DateTime.UtcNow.AddMinutes(-10);
        var remoteModifiedUtc = DateTime.UtcNow.AddMinutes(-5);
        var localSize = 1024L;
        var remoteSize = 2048L;
        var detectedUtc = DateTime.UtcNow;
        var resolutionStrategy = Enums.ConflictResolutionStrategy.None;
        var isResolved = false;

        var syncConflict = SyncConflict.CreateUnresolvedConflict(
            accountId,
            filePath,
            localModifiedUtc,
            remoteModifiedUtc,
            localSize,
            remoteSize);

        syncConflict.Id.ShouldNotBeNull();
        syncConflict.AccountId.ShouldBe(accountId);
        syncConflict.FilePath.ShouldBe(filePath);
        syncConflict.LocalModifiedUtc.ShouldBe(localModifiedUtc);
        syncConflict.RemoteModifiedUtc.ShouldBe(remoteModifiedUtc);
        syncConflict.LocalSize.ShouldBe(localSize);
        syncConflict.RemoteSize.ShouldBe(remoteSize);
        syncConflict.DetectedUtc.ShouldBe(detectedUtc, TimeSpan.FromSeconds(1));
        syncConflict.ResolutionStrategy.ShouldBe(resolutionStrategy);
        syncConflict.IsResolved.ShouldBe(isResolved);
    }
}