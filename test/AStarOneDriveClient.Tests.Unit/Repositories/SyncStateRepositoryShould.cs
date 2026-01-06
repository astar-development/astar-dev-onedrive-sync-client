using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.Repositories;

public class SyncStateRepositoryShould
{
    [Fact]
    public async Task ReturnNullWhenSyncStateNotFound()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);

        var result = await repository.GetByAccountIdAsync("nonexistent");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveNewSyncStateSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);
        var syncState = new SyncState("acc1", SyncStatus.Running, 100, 50, 1024000, 512000, 2, 3, 0, 0, 5.5, 30, DateTime.UtcNow);

        await repository.SaveAsync(syncState);

        var result = await repository.GetByAccountIdAsync("acc1");
        result.ShouldNotBeNull();
        result.Status.ShouldBe(SyncStatus.Running);
        result.TotalFiles.ShouldBe(100);
        result.CompletedFiles.ShouldBe(50);
        result.MegabytesPerSecond.ShouldBe(5.5);
    }

    [Fact]
    public async Task UpdateExistingSyncStateSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);
        var initial = new SyncState("acc1", SyncStatus.Running, 100, 50, 1024000, 512000, 2, 3, 0, 0, 5.5, 30, DateTime.UtcNow);
        await repository.SaveAsync(initial);

        var updated = new SyncState("acc1", SyncStatus.Completed, 100, 100, 1024000, 1024000, 0, 0, 0, 0, 0, null, DateTime.UtcNow);
        await repository.SaveAsync(updated);

        var result = await repository.GetByAccountIdAsync("acc1");
        result.ShouldNotBeNull();
        result.Status.ShouldBe(SyncStatus.Completed);
        result.CompletedFiles.ShouldBe(100);
        result.FilesDownloading.ShouldBe(0);
        result.FilesUploading.ShouldBe(0);
    }

    [Fact]
    public async Task GetAllSyncStatesCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);
        await repository.SaveAsync(new SyncState("acc1", SyncStatus.Running, 10, 5, 1000, 500, 1, 0, 0, 0, 2.5, 10, DateTime.UtcNow));
        await repository.SaveAsync(new SyncState("acc2", SyncStatus.Idle, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null));

        var result = await repository.GetAllAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(s => s.AccountId == "acc1");
        result.ShouldContain(s => s.AccountId == "acc2");
    }

    [Fact]
    public async Task DeleteSyncStateSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);
        await repository.SaveAsync(new SyncState("acc1", SyncStatus.Running, 10, 5, 1000, 500, 1, 0, 0, 0, 2.5, 10, DateTime.UtcNow));

        await repository.DeleteAsync("acc1");

        var result = await repository.GetByAccountIdAsync("acc1");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task NotThrowWhenDeletingNonExistentSyncState()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);

        await Should.NotThrowAsync(async () => await repository.DeleteAsync("nonexistent"));
    }

    [Fact]
    public async Task HandleConflictsDetectedCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);
        var syncState = new SyncState("acc1", SyncStatus.Running, 100, 80, 1024000, 819200, 1, 2, 0, 5, 4.2, 15, DateTime.UtcNow);

        await repository.SaveAsync(syncState);

        var result = await repository.GetByAccountIdAsync("acc1");
        result.ShouldNotBeNull();
        result.ConflictsDetected.ShouldBe(5);
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenSavingNullSyncState()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.SaveAsync(null!)
        );
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullAccountId()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncStateRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.GetByAccountIdAsync(null!)
        );
    }

    private static SyncDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
