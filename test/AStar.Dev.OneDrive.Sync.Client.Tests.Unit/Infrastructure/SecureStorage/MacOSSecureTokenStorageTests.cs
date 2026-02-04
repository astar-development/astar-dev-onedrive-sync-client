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
        var storage = CreateStorage();
        if (!OperatingSystem.IsMacOS())
        {
            storage.IsAvailable.ShouldBeFalse();
        }
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        var storage = CreateStorage();
        storage.Name.ShouldBe("macOS Keychain");
    }

    [Fact]
    public async Task StoreToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        var storage = CreateStorage();

        if (OperatingSystem.IsMacOS())
        {
            return;
        }
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.StoreTokenAsync("key", "token"));
    }

    [Fact]
    public async Task RetrieveToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        var storage = CreateStorage();

        if (OperatingSystem.IsMacOS())
        {
            return;
        }
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.RetrieveTokenAsync("key"));
    }

    [Fact]
    public async Task DeleteToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        var storage = CreateStorage();

        if (OperatingSystem.IsMacOS())
        {
            return;
        }
        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.DeleteTokenAsync("key"));
    }
}