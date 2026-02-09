using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Models;

public class SyncStateShould
{
    [Fact]
    public void ContainTheCreateMethodWhichCreatesTheExpectedInitialState()
    {
        // Arrange
        var accountId = "test-account-id";

        // Act
        var syncState = SyncState.CreateInitial(accountId);

        // Assert
        Assert.Equal(accountId, syncState.AccountId);
        Assert.Equal(SyncStatus.Idle, syncState.Status);
        Assert.Equal(0, syncState.TotalFiles);
        Assert.Equal(0, syncState.CompletedFiles);
        Assert.Equal(0L, syncState.TotalBytes);
        Assert.Equal(0L, syncState.CompletedBytes);
        Assert.Equal(0, syncState.FilesDownloading);
        Assert.Equal(0, syncState.FilesUploading);
        Assert.Equal(0, syncState.FilesDeleted);
        Assert.Equal(0, syncState.ConflictsDetected);
        Assert.Equal(0.0, syncState.MegabytesPerSecond);
        syncState.EstimatedSecondsRemaining.ShouldBe(0);
        syncState.CurrentStatusMessage.ShouldBe(string.Empty);
        Assert.Null(syncState.LastUpdateUtc);
    }
}
