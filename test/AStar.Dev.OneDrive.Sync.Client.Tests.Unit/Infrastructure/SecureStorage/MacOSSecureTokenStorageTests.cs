using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

public class MacOSSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    protected override ISecureTokenStorage CreateStorage() => new MacOSSecureTokenStorage();

    [Fact]
    public void IsAvailable_OnMacOS_DependsOnSecurityCommand()
    {
        ISecureTokenStorage storage = CreateStorage();
        if(!OperatingSystem.IsMacOS())
        {
            storage.IsAvailable.ShouldBeFalse();
        }
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        ISecureTokenStorage storage = CreateStorage();
        storage.Name.ShouldBe("macOS Keychain");
    }

    [Fact]
    public async Task StoreToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        ISecureTokenStorage storage = CreateStorage();

        if(OperatingSystem.IsMacOS())
        {
            return;
        }

        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.StoreTokenAsync("key", "token"));
    }

    [Fact]
    public async Task RetrieveToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        ISecureTokenStorage storage = CreateStorage();

        if(OperatingSystem.IsMacOS())
        {
            return;
        }

        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.RetrieveTokenAsync("key"));
    }

    [Fact]
    public async Task DeleteToken_OnNonMacOS_ThrowsPlatformNotSupportedException()
    {
        ISecureTokenStorage storage = CreateStorage();

        if(OperatingSystem.IsMacOS())
        {
            return;
        }

        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.DeleteTokenAsync("key"));
    }
}
