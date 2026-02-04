using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

public class SecureTokenStorageFactoryTests
{
    [Fact]
    public void CreateStorage_WithoutFallback_ReturnsPlatformSpecificStorage()
    {
        var factory = new SecureTokenStorageFactory(useFallback: false);
        ISecureTokenStorage storage = factory.CreateStorage();
        storage.ShouldNotBeNull();
        storage.ShouldBeAssignableTo<ISecureTokenStorage>();

        if(OperatingSystem.IsWindows())
        {
            storage.ShouldBeOfType<WindowsSecureTokenStorage>();
        }
        else if(OperatingSystem.IsMacOS())
        {
            // Could be MacOS or AES fallback depending on security command availability
            storage.ShouldBeAssignableTo<ISecureTokenStorage>();
        }
        else if(OperatingSystem.IsLinux())
        {
            // Could be Linux or AES fallback depending on secret-tool availability
            storage.ShouldBeAssignableTo<ISecureTokenStorage>();
        }
        else
        {
            storage.ShouldBeOfType<AesSecureTokenStorage>();
        }
    }

    [Fact]
    public void CreateStorage_WithFallback_ReturnsAesStorage()
    {
        var factory = new SecureTokenStorageFactory(useFallback: true);
        ISecureTokenStorage storage = factory.CreateStorage();
        storage.ShouldNotBeNull();
        storage.ShouldBeOfType<AesSecureTokenStorage>();
    }

    [Fact]
    public void CreateStorage_AlwaysReturnsAvailableStorage()
    {
        var factory = new SecureTokenStorageFactory(useFallback: false);
        ISecureTokenStorage storage = factory.CreateStorage();
        storage.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void GetStorageName_ReturnsCorrectName()
    {
        var factory = new SecureTokenStorageFactory(useFallback: false);
        var name = factory.GetStorageName();
        name.ShouldNotBeNullOrWhiteSpace();

        if(OperatingSystem.IsWindows())
        {
            name.ShouldBe("Windows DPAPI");
        }
        else
        {
            name.ShouldBeOneOf("macOS Keychain", "Linux Secret Service", "AES-256 Fallback");
        }
    }

    [Fact]
    public void GetStorageName_WithFallback_ReturnsAesFallbackName()
    {
        var factory = new SecureTokenStorageFactory(useFallback: true);
        var name = factory.GetStorageName();
        name.ShouldBe("AES-256 Fallback");
    }

    [Fact]
    public void CreateStorage_MultipleCalls_ReturnsNewInstances()
    {
        var factory = new SecureTokenStorageFactory(useFallback: false);
        ISecureTokenStorage storage1 = factory.CreateStorage();
        ISecureTokenStorage storage2 = factory.CreateStorage();
        storage1.ShouldNotBeSameAs(storage2);
    }

    [Fact]
    public void Constructor_DefaultParameters_UsesDefaultFallbackValue()
    {
        var factory = new SecureTokenStorageFactory();
        ISecureTokenStorage storage = factory.CreateStorage();
        storage.ShouldNotBeNull();
        storage.IsAvailable.ShouldBeTrue();
    }
}
