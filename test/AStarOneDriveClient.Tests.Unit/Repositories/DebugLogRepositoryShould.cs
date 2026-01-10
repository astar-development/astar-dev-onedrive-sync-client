using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.Repositories;

public class DebugLogRepositoryShould
{
    [Fact]
    public async Task GetByAccountIdWithPagingReturnsCorrectRecords()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 10);

        var result = await repository.GetByAccountIdAsync("acc1", 5, 0, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetByAccountIdWithPagingSkipsCorrectRecords()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 10);

        var result = await repository.GetByAccountIdAsync("acc1", 5, 5, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetByAccountIdReturnsAllRecordsForAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 15);
        await SeedDebugLogsAsync(context, "acc2", 5);

        var result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(15);
        result.All(log => log.AccountId == "acc1").ShouldBeTrue();
    }

    [Fact]
    public async Task GetByAccountIdReturnsRecordsOrderedByTimestampDescending()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);

        // Add logs with different timestamps
        context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = DateTime.UtcNow.AddHours(-2),
            LogLevel = "Info",
            Source = "Test",
            Message = "Oldest"
        });
        context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = DateTime.UtcNow,
            LogLevel = "Info",
            Source = "Test",
            Message = "Newest"
        });
        context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = DateTime.UtcNow.AddHours(-1),
            LogLevel = "Info",
            Source = "Test",
            Message = "Middle"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        result[0].Message.ShouldBe("Newest");
        result[1].Message.ShouldBe("Middle");
        result[2].Message.ShouldBe("Oldest");
    }

    [Fact]
    public async Task DeleteByAccountIdRemovesOnlySpecifiedAccountLogs()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 5);
        await SeedDebugLogsAsync(context, "acc2", 3);

        await repository.DeleteByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        var acc1Logs = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        var acc2Logs = await repository.GetByAccountIdAsync("acc2", TestContext.Current.CancellationToken);
        acc1Logs.ShouldBeEmpty();
        acc2Logs.Count.ShouldBe(3);
    }

    [Fact]
    public async Task DeleteOlderThanRemovesOldRecords()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);

        var cutoff = DateTime.UtcNow.AddDays(-7);

        context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = cutoff.AddDays(-1),
            LogLevel = "Info",
            Source = "Test",
            Message = "Old"
        });
        context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = cutoff.AddDays(1),
            LogLevel = "Info",
            Source = "Test",
            Message = "Recent"
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteOlderThanAsync(cutoff, TestContext.Current.CancellationToken);

        var result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        result.Count.ShouldBe(1);
        result[0].Message.ShouldBe("Recent");
    }

    [Fact]
    public async Task GetByAccountIdReturnsEmptyListWhenNoRecordsExist()
    {
        using var context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);

        var result = await repository.GetByAccountIdAsync("nonexistent", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    private static SyncDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SyncDbContext(options);
    }

    private static async Task SeedDebugLogsAsync(SyncDbContext context, string accountId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            context.DebugLogs.Add(new DebugLogEntity
            {
                AccountId = accountId,
                TimestampUtc = DateTime.UtcNow.AddMinutes(-i),
                LogLevel = "Info",
                Source = $"Test.Method{i}",
                Message = $"Log message {i}"
            });
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
