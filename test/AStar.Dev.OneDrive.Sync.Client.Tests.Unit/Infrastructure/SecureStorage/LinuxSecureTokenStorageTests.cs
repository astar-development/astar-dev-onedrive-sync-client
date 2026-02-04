using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

public class LinuxSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    protected override ISecureTokenStorage CreateStorage() => new LinuxSecureTokenStorage();

    [Fact]
    public void IsAvailable_OnLinux_DependsOnSecretTool()
    {
        ISecureTokenStorage storage = CreateStorage();
        if(!OperatingSystem.IsLinux())
        {
            storage.IsAvailable.ShouldBeFalse();
        }
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        ISecureTokenStorage storage = CreateStorage();
        storage.Name.ShouldBe("Linux Secret Service");
    }

    [Fact]
    public async Task StoreToken_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        ISecureTokenStorage storage = CreateStorage();

        if(OperatingSystem.IsLinux())
        {
            return;
        }

        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.StoreTokenAsync("key", "token"));
    }

    [Fact]
    public async Task RetrieveToken_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        ISecureTokenStorage storage = CreateStorage();

        if(OperatingSystem.IsLinux())
        {
            return;
        }

        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.RetrieveTokenAsync("key"));
    }

    [Fact]
    public async Task DeleteToken_OnNonLinux_ThrowsPlatformNotSupportedException()
    {
        ISecureTokenStorage storage = CreateStorage();

        if(OperatingSystem.IsLinux())
        {
            return;
        }

        await Should.ThrowAsync<PlatformNotSupportedException>(async () =>
            await storage.DeleteTokenAsync("key"));
    }
}
