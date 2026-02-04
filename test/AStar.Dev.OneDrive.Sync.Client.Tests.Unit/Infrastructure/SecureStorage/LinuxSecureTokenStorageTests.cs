using Shouldly;
using Xunit;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

/// <summary>
/// Tests for Linux Secret Service secure token storage.
/// </summary>
public class LinuxSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    protected override ISecureTokenStorage CreateStorage()
    {
        return new LinuxSecureTokenStorage();
    }

    [Fact]
    public void IsAvailable_OnLinux_DependsOnSecretTool()
    {
        // Arrange
        var storage = CreateStorage();

        // Assert
        if (!OperatingSystem.IsLinux())
        {
            storage.IsAvailable.ShouldBeFalse();
        }
        // On Linux, availability depends on secret-tool being present
        // We can't assert true/false without knowing the system state
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var storage = CreateStorage();

        // Assert
        storage.Name.ShouldBe("Linux Secret Service");
    }

    [Fact]
    public async Task StoreToken_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        var storage = CreateStorage();

        if (OperatingSystem.IsLinux())
        {
            // Skip this test on Linux
            return;
        }

        // Act & Assert
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.StoreTokenAsync("key", "token"));
    }

    [Fact]
    public async Task RetrieveToken_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        var storage = CreateStorage();

        if (OperatingSystem.IsLinux())
        {
            // Skip this test on Linux
            return;
        }

        // Act & Assert
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.RetrieveTokenAsync("key"));
    }

    [Fact]
    public async Task DeleteToken_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        // Arrange
        var storage = CreateStorage();

        if (OperatingSystem.IsLinux())
        {
            // Skip this test on Linux
            return;
        }

        // Act & Assert
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.DeleteTokenAsync("key"));
    }
}