using AStar.Dev.OneDrive.Sync.Client.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.ConfigurationSettings;

public class ApplicationSettingsShould
{
    [Fact]
    public void HaveExpectedDefaultValues()
    {
        var settings = new ApplicationSettings();

        settings.ToJson().ShouldMatchApproved();
    }
}
