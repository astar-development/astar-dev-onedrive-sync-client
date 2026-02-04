
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Common.Models;
public class AccountSettingsShould
{
    [Fact]
    public void CreateSettingsWithAllProperties()
    {
        // Arrange
        const string homeSyncDirectory = "/home/user/OneDrive";
        const int maxConcurrent = 10;
        const bool debugLoggingEnabled = true;

        // Act
        var settings = new AccountSettings(homeSyncDirectory, maxConcurrent, debugLoggingEnabled);

        // Assert
        settings.HomeSyncDirectory.ShouldBe(homeSyncDirectory);
        settings.MaxConcurrent.ShouldBe(maxConcurrent);
        settings.DebugLoggingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AllowEmptyHomeSyncDirectory()
    {
        // Act
        var settings = new AccountSettings(string.Empty, 5, false);

        // Assert
        settings.HomeSyncDirectory.ShouldBe(string.Empty);
    }

    [Fact]
    public void AllowNullHomeSyncDirectory()
    {
        // Act
        var settings = new AccountSettings(null, 5, false);

        // Assert
        settings.HomeSyncDirectory.ShouldBeNull();
    }

    [Fact]
    public void HaveRecordEqualitySemantics()
    {
        // Arrange
        var settings1 = new AccountSettings("/sync", 5, true);
        var settings2 = new AccountSettings("/sync", 5, true);

        // Act & Assert
        settings1.ShouldBe(settings2);
    }

    [Fact]
    public void HaveRecordInequalityForDifferentValues()
    {
        // Arrange
        var settings1 = new AccountSettings("/sync1", 5, true);
        var settings2 = new AccountSettings("/sync2", 5, true);

        // Act & Assert
        settings1.ShouldNotBe(settings2);
    }

    [Fact]
    public void SupportWithExpressionForCopy()
    {
        // Arrange
        var originalSettings = new AccountSettings("/sync", 5, false);

        AccountSettings updatedSettings = originalSettings with { MaxConcurrent = 10 };

        // Assert
        updatedSettings.HomeSyncDirectory.ShouldBe("/sync");
        updatedSettings.MaxConcurrent.ShouldBe(10);
        updatedSettings.DebugLoggingEnabled.ShouldBeFalse();
        originalSettings.MaxConcurrent.ShouldBe(5); // Original unchanged
    }

    [Fact]
    public void SupportWithExpressionForMultipleProperties()
    {
        // Arrange
        var originalSettings = new AccountSettings("/sync", 5, false);

        AccountSettings updatedSettings = originalSettings with
        {
            HomeSyncDirectory = "/new/sync",
            MaxConcurrent = 15,
            DebugLoggingEnabled = true
        };

        // Assert
        updatedSettings.HomeSyncDirectory.ShouldBe("/new/sync");
        updatedSettings.MaxConcurrent.ShouldBe(15);
        updatedSettings.DebugLoggingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AllowAnyStringForHomeSyncDirectory()
    {
        // Arrange
        const string invalidPath = "anything/goes/here";

        // Act
        var settings = new AccountSettings(invalidPath, 5, false);

        // Assert
        settings.HomeSyncDirectory.ShouldBe(invalidPath);
    }

    [Fact]
    public void AllowAnyIntegerForMaxConcurrent()
    {
        // Arrange & Act
        var settings1 = new AccountSettings("/sync", 1, false);
        var settings2 = new AccountSettings("/sync", 100, false);

        // Assert
        settings1.MaxConcurrent.ShouldBe(1);
        settings2.MaxConcurrent.ShouldBe(100);
    }

    [Fact]
    public void HaveConsistentHashCode()
    {
        // Arrange
        var settings1 = new AccountSettings("/sync", 5, true);
        var settings2 = new AccountSettings("/sync", 5, true);

        // Act & Assert
        settings1.GetHashCode().ShouldBe(settings2.GetHashCode());
    }

    [Fact]
    public void HaveDifferentHashCodeForDifferentValues()
    {
        // Arrange
        var settings1 = new AccountSettings("/sync1", 5, true);
        var settings2 = new AccountSettings("/sync2", 5, true);

        // Act & Assert
        settings1.GetHashCode().ShouldNotBe(settings2.GetHashCode());
    }

    [Fact]
    public void BeComparableInCollections()
    {
        // Arrange
        var settings1 = new AccountSettings("/sync", 5, true);
        var settings2 = new AccountSettings("/sync", 5, true);
        var settingsList = new List<AccountSettings> { settings1 };

        // Act & Assert
        settingsList.Contains(settings2).ShouldBeTrue();
    }
}
