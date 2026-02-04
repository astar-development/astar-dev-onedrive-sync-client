using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Models;

public class AccountSettingsShould
{
    [Fact]
    public void CreateSettingsWithAllProperties()
    {
        const string homeSyncDirectory = "/home/user/OneDrive";
        const int maxConcurrent = 10;
        const bool debugLoggingEnabled = true;

        var settings = new AccountSettings(homeSyncDirectory, maxConcurrent, debugLoggingEnabled);

        settings.HomeSyncDirectory.ShouldBe(homeSyncDirectory);
        settings.MaxConcurrent.ShouldBe(maxConcurrent);
        settings.DebugLoggingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AllowEmptyHomeSyncDirectory()
    {
        var settings = new AccountSettings(string.Empty, 5, false);

        settings.HomeSyncDirectory.ShouldBe(string.Empty);
    }

    [Fact]
    public void AllowNullHomeSyncDirectory()
    {
        var settings = new AccountSettings(null, 5, false);

        settings.HomeSyncDirectory.ShouldBeNull();
    }

    [Fact]
    public void HaveRecordEqualitySemantics()
    {
        var settings1 = new AccountSettings("/sync", 5, true);
        var settings2 = new AccountSettings("/sync", 5, true);

        settings1.ShouldBe(settings2);
    }

    [Fact]
    public void HaveRecordInequalityForDifferentValues()
    {
        var settings1 = new AccountSettings("/sync1", 5, true);
        var settings2 = new AccountSettings("/sync2", 5, true);

        settings1.ShouldNotBe(settings2);
    }

    [Fact]
    public void SupportWithExpressionForCopy()
    {
        var originalSettings = new AccountSettings("/sync", 5, false);

        AccountSettings updatedSettings = originalSettings with { MaxConcurrent = 10 };

        updatedSettings.HomeSyncDirectory.ShouldBe("/sync");
        updatedSettings.MaxConcurrent.ShouldBe(10);
        updatedSettings.DebugLoggingEnabled.ShouldBeFalse();
        originalSettings.MaxConcurrent.ShouldBe(5); // Original unchanged
    }

    [Fact]
    public void SupportWithExpressionForMultipleProperties()
    {
        var originalSettings = new AccountSettings("/sync", 5, false);

        AccountSettings updatedSettings = originalSettings with
        {
            HomeSyncDirectory = "/new/sync",
            MaxConcurrent = 15,
            DebugLoggingEnabled = true
        };

        updatedSettings.HomeSyncDirectory.ShouldBe("/new/sync");
        updatedSettings.MaxConcurrent.ShouldBe(15);
        updatedSettings.DebugLoggingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AllowAnyStringForHomeSyncDirectory()
    {
        const string invalidPath = "anything/goes/here";

        var settings = new AccountSettings(invalidPath, 5, false);

        settings.HomeSyncDirectory.ShouldBe(invalidPath);
    }

    [Fact]
    public void AllowAnyIntegerForMaxConcurrent()
    {
        var settings1 = new AccountSettings("/sync", 1, false);
        var settings2 = new AccountSettings("/sync", 100, false);

        settings1.MaxConcurrent.ShouldBe(1);
        settings2.MaxConcurrent.ShouldBe(100);
    }

    [Fact]
    public void HaveConsistentHashCode()
    {
        var settings1 = new AccountSettings("/sync", 5, true);
        var settings2 = new AccountSettings("/sync", 5, true);

        settings1.GetHashCode().ShouldBe(settings2.GetHashCode());
    }

    [Fact]
    public void HaveDifferentHashCodeForDifferentValues()
    {
        var settings1 = new AccountSettings("/sync1", 5, true);
        var settings2 = new AccountSettings("/sync2", 5, true);

        settings1.GetHashCode().ShouldNotBe(settings2.GetHashCode());
    }

    [Fact]
    public void BeComparableInCollections()
    {
        var settings1 = new AccountSettings("/sync", 5, true);
        var settings2 = new AccountSettings("/sync", 5, true);
        var settingsList = new List<AccountSettings> { settings1 };

        settingsList.Contains(settings2).ShouldBeTrue();
    }
}
