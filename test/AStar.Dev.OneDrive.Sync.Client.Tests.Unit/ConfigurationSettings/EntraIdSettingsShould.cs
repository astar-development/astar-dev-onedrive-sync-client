using AStar.Dev.OneDrive.Sync.Client.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.ConfigurationSettings;

public class EntraIdSettingsShould
{
    [Fact]
    public void HaveExpectedDefaultValues()
    {
        var settings = new EntraIdSettings();

        settings.ToJson().ShouldMatchApproved();
    }
}
