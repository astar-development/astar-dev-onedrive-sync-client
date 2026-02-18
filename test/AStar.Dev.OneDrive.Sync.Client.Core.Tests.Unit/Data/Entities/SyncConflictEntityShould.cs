using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data.Entities;

public class SyncConflictEntityShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        var syncConflictEntity = new SyncConflictEntity
        {
            Id = "conflict-id",
            HashedAccountId = new HashedAccountId("hashed-account-id"),
            AccountId = "acc-item-id",
            FilePath = "relative/path/to/item",
            LocalModifiedUtc = currentTime,
            RemoteModifiedUtc = currentTime,
            LocalSize = 1024,
            RemoteSize = 2048,
            DetectedUtc = currentTime,
            ResolutionStrategy = ConflictResolutionStrategy.KeepLocal,
            IsResolved = false,
            Account = new AccountEntity
            {
                Id = "account-id",
                HashedAccountId = new HashedAccountId("hashed-account-id"),
                DisplayName = "Test Account",
                LocalSyncPath = "/path/to/sync",
                IsAuthenticated = true,
                LastSyncUtc = currentTime,
                DeltaToken = "delta-token",
                EnableDetailedSyncLogging = true,
                EnableDebugLogging = false,
                MaxParallelUpDownloads = 3,
                MaxItemsInBatch = 100
            }
        };

        syncConflictEntity.Id.ShouldBe("conflict-id");
        syncConflictEntity.HashedAccountId.Value.ShouldBe("hashed-account-id");
        syncConflictEntity.AccountId.ShouldBe("acc-item-id");
        syncConflictEntity.FilePath.ShouldBe("relative/path/to/item");
        syncConflictEntity.LocalModifiedUtc.ShouldBe(currentTime);
        syncConflictEntity.RemoteModifiedUtc.ShouldBe(currentTime);
        syncConflictEntity.LocalSize.ShouldBe(1024);
        syncConflictEntity.RemoteSize.ShouldBe(2048);
        syncConflictEntity.DetectedUtc.ShouldBe(currentTime);
        syncConflictEntity.ResolutionStrategy.ShouldBe(ConflictResolutionStrategy.KeepLocal);
        syncConflictEntity.IsResolved.ShouldBeFalse();
        syncConflictEntity.Account.ShouldNotBeNull();
        syncConflictEntity.Account.Id.ShouldBe("account-id");
        syncConflictEntity.Account.HashedAccountId.Value.ShouldBe("hashed-account-id");
        syncConflictEntity.Account.DisplayName.ShouldBe("Test Account");
        syncConflictEntity.Account.LocalSyncPath.ShouldBe("/path/to/sync");
        syncConflictEntity.Account.IsAuthenticated.ShouldBeTrue();
        syncConflictEntity.Account.LastSyncUtc.ShouldBe(currentTime);
        syncConflictEntity.Account.DeltaToken.ShouldBe("delta-token");
        syncConflictEntity.Account.EnableDetailedSyncLogging.ShouldBeTrue();
        syncConflictEntity.Account.EnableDebugLogging.ShouldBeFalse();
        syncConflictEntity.Account.MaxParallelUpDownloads.ShouldBe(3);
        syncConflictEntity.Account.MaxItemsInBatch.ShouldBe(100);
    }
}
