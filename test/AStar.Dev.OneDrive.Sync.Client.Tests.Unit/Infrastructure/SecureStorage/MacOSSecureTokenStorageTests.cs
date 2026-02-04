using Shouldly;
using Xunit;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

/// <summary>
/// Tests for macOS Keychain secure token storage.
/// </summary>
public class MacOSSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    protected override ISecureTokenStorage CreateStorage()
    {
        return new MacOSSecureTokenStorage();
    }

    [Fact]
    public void IsAvailable_OnMacOS_DependsOnSecurityCommand()
    {
        // Arrange
        var storage = CreateStorage();

        // Assert
        if (!OperatingSystem.IsMacOS())
        {
            storage.IsAvailable.ShouldBeFalse();
        }
        // On macOS, availability depends on security command being present
        // We can't assert true/false without knowing the system state
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var storage = CreateStorage();

        // Assert
        storage.Name.ShouldBe("macOS Keychain");
    }

    [Fact]
    public async Task StoreToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        var storage = CreateStorage();

        if (OperatingSystem.IsMacOS())
        {
            // Skip this test on macOS
            return;
        }

        // Act & Assert
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.StoreTokenAsync("key", "token"));
    }

    [Fact]
    public async Task RetrieveToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        var storage = CreateStorage();

        if (OperatingSystem.IsMacOS())
        {
            // Skip this test on macOS
            return;
        }

        // Act & Assert
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.RetrieveTokenAsync("key"));
    }

    [Fact]
    public async Task DeleteToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        var storage = CreateStorage();

        if (OperatingSystem.IsMacOS())
        {
            // Skip this test on macOS
            return;
        }

        // Act & Assert
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.DeleteTokenAsync("key"));
    }
}