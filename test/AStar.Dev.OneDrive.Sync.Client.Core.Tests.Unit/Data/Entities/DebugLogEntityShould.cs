using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data.Entities;

public class DebugLogEntityShould
{
    [Fact]
    public void ContainTheExpectedProperties()
    {
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        var debugLogEntity = new DebugLogEntity
        {
            Id = 1,
            HashedAccountId = new HashedAccountId("hashed-account-id"),
            TimestampUtc = currentTime,
            LogLevel = "Information",
            Source = "TestSource",
            Message = "This is a test log message.",
            Exception = "System.Exception: Test exception"
        };

        debugLogEntity.Id.ShouldBe(1);
        debugLogEntity.HashedAccountId.Value.ShouldBe("hashed-account-id");
        debugLogEntity.TimestampUtc.ShouldBe(currentTime);
        debugLogEntity.LogLevel.ShouldBe("Information");
        debugLogEntity.Source.ShouldBe("TestSource");
        debugLogEntity.Message.ShouldBe("This is a test log message.");
        debugLogEntity.Exception.ShouldBe("System.Exception: Test exception");
    }
}
