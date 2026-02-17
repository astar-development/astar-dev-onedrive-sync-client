using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Models;

public sealed class HashedAccountIdShould
{
    [Fact]
    public void ConvertImplicitlyToAndFromString()
    {
        var id = new HashedAccountId("acc-123");

        var value = id.Value;

        value.ShouldBe("acc-123");
    }
}
