using System.Runtime.InteropServices;
using AStar.Dev.OneDrive.Sync.Client.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.ConfigurationSettings;

public class AppPathHelperShould
{
    [Fact]
    public void ReturnExpectedAppDataPath()
    {
        var appName = "TestApp";
        var expectedPath = GetExpectedAppDataPath(appName);

        var actualPath = AppPathHelper.GetAppDataPath(appName);

        actualPath.ShouldBe(expectedPath);
    }

    private static string GetExpectedAppDataPath(string appName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, appName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, "Library", "Application Support", appName);
        }

        var linuxHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(linuxHomeDir, ".config", appName);
    }
}
