using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Models;

public class SyncConflictShould
{
    [Fact]
    public void CreateUnresolvedInstanceCorrectly()
    {
        var accountId = "account-id";
        var filePath = "/path/to/conflicted/file.txt";
        DateTimeOffset localModifiedUtc = DateTime.UtcNow.AddMinutes(-10);
        DateTimeOffset remoteModifiedUtc = DateTime.UtcNow.AddMinutes(-5);
        var localSize = 1024L;
        var remoteSize = 2048L;
        DateTimeOffset detectedUtc = DateTime.UtcNow;
        ConflictResolutionStrategy resolutionStrategy = ConflictResolutionStrategy.None;
        var isResolved = false;

        var syncConflict = SyncConflict.CreateUnresolvedConflict(accountId, new HashedAccountId(AccountIdHasher.Hash(accountId)),filePath,localModifiedUtc,remoteModifiedUtc,localSize,remoteSize);

        _ = syncConflict.Id.ShouldNotBeNull();
        syncConflict.AccountId.ShouldBe(accountId);
        syncConflict.HashedAccountId.Value.ShouldBe(AccountIdHasher.Hash(accountId));
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
