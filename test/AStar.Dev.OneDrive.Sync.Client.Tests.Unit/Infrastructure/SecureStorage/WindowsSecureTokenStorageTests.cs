using System.Runtime.Versioning;
using Shouldly;
using Xunit;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

/// <summary>
/// Tests for Windows DPAPI secure token storage.
/// </summary>
public class WindowsSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    protected override ISecureTokenStorage CreateStorage()
    {
        return new WindowsSecureTokenStorage();
    }

    [Fact]
    public void IsAvailable_OnWindows_ReturnsTrue()
    {
        var storage = CreateStorage();
        if (OperatingSystem.IsWindows())
        {
            storage.IsAvailable.ShouldBeTrue();
        }
        else
        {
            storage.IsAvailable.ShouldBeFalse();
        }
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        var storage = CreateStorage();
        storage.Name.ShouldBe("Windows DPAPI");
    }

    [Fact]
    public async Task StoreToken_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        var storage = CreateStorage();

        if (OperatingSystem.IsWindows())
        {
            return;
        }
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.StoreTokenAsync("key", "token"));
    }

    [Fact]
    public async Task RetrieveToken_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        var storage = CreateStorage();

        if (OperatingSystem.IsWindows())
        {
            return;
        }
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.RetrieveTokenAsync("key"));
    }

    [Fact]
    public async Task DeleteToken_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        var storage = CreateStorage();

        if (OperatingSystem.IsWindows())
        {
            return;
        }
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.DeleteTokenAsync("key"));
    }
}