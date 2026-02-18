using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data.Entities;

public class AccountEntityShould
{
    [Fact]
    public void CreateSystemAccountCorrectly()
    {
        var systemAccount = AccountEntity.CreateSystemAccount();

        _ = systemAccount.Id.ShouldNotBeNull();
        systemAccount.HashedAccountId.Value.ShouldBe(AccountIdHasher.Hash(AdminAccountMetadata.Id));
        systemAccount.DisplayName.ShouldBe("System Admin");
        systemAccount.LocalSyncPath.ShouldBe(".");
        systemAccount.AutoSyncIntervalMinutes.ShouldBe(0);
        systemAccount.DeltaToken.ShouldBeNull();
        systemAccount.EnableDebugLogging.ShouldBeTrue();
        systemAccount.EnableDetailedSyncLogging.ShouldBeTrue();
        systemAccount.IsAuthenticated.ShouldBeTrue();
        systemAccount.LastSyncUtc.ShouldBeNull();
        systemAccount.MaxItemsInBatch.ShouldBe(1);
        systemAccount.MaxParallelUpDownloads.ShouldBe(1);
    }

    [Fact]
    public void ContainTheExpectedProperties()
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        var accountEntity = new AccountEntity
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
        };

        accountEntity.Id.ShouldBe("account-id");
        accountEntity.HashedAccountId.Value.ShouldBe("hashed-account-id");
        accountEntity.DisplayName.ShouldBe("Test Account");
        accountEntity.LocalSyncPath.ShouldBe("/path/to/sync");
        accountEntity.IsAuthenticated.ShouldBeTrue();
        accountEntity.LastSyncUtc.ShouldBe(currentTime);
        accountEntity.DeltaToken.ShouldBe("delta-token");
        accountEntity.EnableDetailedSyncLogging.ShouldBeTrue();
        accountEntity.EnableDebugLogging.ShouldBeFalse();
        accountEntity.MaxParallelUpDownloads.ShouldBe(3);
        accountEntity.MaxItemsInBatch.ShouldBe(100);
    }
}
