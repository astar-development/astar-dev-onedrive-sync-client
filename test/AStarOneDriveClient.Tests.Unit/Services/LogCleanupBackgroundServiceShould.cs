using System;
using System.Threading;
using System.Threading.Tasks;
using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Testably.Abstractions.Testing;
using Xunit;

namespace AStarOneDriveClient.Tests.Unit.Services;

public class LogCleanupBackgroundServiceShould
{
    private static (LogCleanupBackgroundService, SyncDbContext, TestLogger) CreateServiceWithDb(params object[] seedEntities)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SyncDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<SyncDbContext>();
        foreach (var entity in seedEntities)
        {
            switch (entity)
            {
                case SyncSessionLogEntity session: db.SyncSessionLogs.Add(session); break;
                case DebugLogEntity debug: db.DebugLogs.Add(debug); break;
            }
        }
        db.SaveChanges();
        var logger = new TestLogger();
        var service = new LogCleanupBackgroundService(provider, logger);
        return (service, db, logger);
    }

    [Fact]
    public async Task Delete_Old_Entries_Only()
    {
        // Arrange
        var oldSession = new SyncSessionLogEntity { Id = "1", AccountId = "A", StartedUtc = DateTime.UtcNow.AddDays(-20) };
        var newSession = new SyncSessionLogEntity { Id = "2", AccountId = "A", StartedUtc = DateTime.UtcNow };
        var oldDebug = new DebugLogEntity { Id = 1, AccountId = "A", TimestampUtc = DateTime.UtcNow.AddDays(-20), LogLevel = "Info", Source = "Test", Message = "Old" };
        var newDebug = new DebugLogEntity { Id = 2, AccountId = "A", TimestampUtc = DateTime.UtcNow, LogLevel = "Info", Source = "Test", Message = "New" };
        var (service, db, logger) = CreateServiceWithDb(oldSession, newSession, oldDebug, newDebug);

        // Act
        await service.TestCleanupOnce();

        // Assert
        (await db.SyncSessionLogs.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await db.DebugLogs.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await db.SyncSessionLogs.FirstAsync(TestContext.Current.CancellationToken)).Id.ShouldBe("2");
        (await db.DebugLogs.FirstAsync(TestContext.Current.CancellationToken)).Id.ShouldBe(2);
        logger.Infos.Count.ShouldBe(1);
        logger.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handles_Exceptions_And_Logs_Error()
    {
        // Arrange
        var provider = Substitute.For<IServiceProvider>();
        provider.GetRequiredService<IServiceScopeFactory>().Returns(_ => throw new Exception("fail"));
        var logger = new TestLogger();
        var service = new LogCleanupBackgroundService(provider, logger);

        // Act
        await service.TestCleanupOnce();

        // Assert
        logger.Errors.Count.ShouldBe(1);
        logger.Infos.Count.ShouldBe(0);
        logger.Errors[0].ShouldContain("fail");
    }

    // Helper logger for assertions
    private class TestLogger : ILogger<LogCleanupBackgroundService>
    {
        public readonly List<string> Infos = new();
        public readonly List<string> Errors = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Substitute.For<IDisposable>();
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information) Infos.Add(formatter(state, exception));
            if (logLevel == LogLevel.Error) Errors.Add(formatter(state, exception) + (exception != null ? $": {exception.Message}" : ""));
        }
    }
}

// Extension for testability
public static class LogCleanupBackgroundServiceTestExtensions
{
    public static async Task TestCleanupOnce(this LogCleanupBackgroundService service)
    {
        // Use reflection to call the private ExecuteAsync logic once, but only the cleanup logic
        var method = typeof(LogCleanupBackgroundService).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var token = TestContext.Current.CancellationToken;
        if (method != null)
        {
            var result = method.Invoke(service, new object[] { token });
            if (result is Task task)
            {
                try { await task; } catch { }
            }
        }
    }
}
