using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Models;

public sealed class HashedAccountIdShould
{
    [Fact]
    public void ConvertImplicitlyToAndFromString()
    {
        HashedAccountId id = "acc-123";

        string value = id;

        value.ShouldBe("acc-123");
    }
}
