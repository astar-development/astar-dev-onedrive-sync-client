using Shouldly;
using Xunit;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

/// <summary>
/// Tests for the secure token storage factory.
/// </summary>
public class SecureTokenStorageFactoryTests
{
    [Fact]
    public void CreateStorage_WithoutFallback_ReturnsPlatformSpecificStorage()
    {
        // Arrange
        var factory = new SecureTokenStorageFactory(useFallback: false);

        // Act
        var storage = factory.CreateStorage();

        // Assert
        storage.ShouldNotBeNull();
        storage.ShouldBeAssignableTo<ISecureTokenStorage>();

        if (OperatingSystem.IsWindows())
        {
            storage.ShouldBeOfType<WindowsSecureTokenStorage>();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Could be MacOS or AES fallback depending on security command availability
            storage.ShouldBeAssignableTo<ISecureTokenStorage>();
        }
        else if (OperatingSystem.IsLinux())
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
        // Arrange
        var factory = new SecureTokenStorageFactory(useFallback: true);

        // Act
        var storage = factory.CreateStorage();

        // Assert
        storage.ShouldNotBeNull();
        storage.ShouldBeOfType<AesSecureTokenStorage>();
    }

    [Fact]
    public void CreateStorage_AlwaysReturnsAvailableStorage()
    {
        // Arrange
        var factory = new SecureTokenStorageFactory(useFallback: false);

        // Act
        var storage = factory.CreateStorage();

        // Assert
        storage.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void GetStorageName_ReturnsCorrectName()
    {
        // Arrange
        var factory = new SecureTokenStorageFactory(useFallback: false);

        // Act
        var name = factory.GetStorageName();

        // Assert
        name.ShouldNotBeNullOrWhiteSpace();

        if (OperatingSystem.IsWindows())
        {
            name.ShouldBe("Windows DPAPI");
        }
        else
        {
            // On non-Windows, could be platform-specific or AES fallback
            name.ShouldBeOneOf("macOS Keychain", "Linux Secret Service", "AES-256 Fallback");
        }
    }

    [Fact]
    public void GetStorageName_WithFallback_ReturnsAesFallbackName()
    {
        // Arrange
        var factory = new SecureTokenStorageFactory(useFallback: true);

        // Act
        var name = factory.GetStorageName();

        // Assert
        name.ShouldBe("AES-256 Fallback");
    }

    [Fact]
    public void CreateStorage_MultipleCalls_ReturnsNewInstances()
    {
        // Arrange
        var factory = new SecureTokenStorageFactory(useFallback: false);

        // Act
        var storage1 = factory.CreateStorage();
        var storage2 = factory.CreateStorage();

        // Assert
        storage1.ShouldNotBeSameAs(storage2);
    }

    [Fact]
    public void Constructor_DefaultParameters_UsesDefaultFallbackValue()
    {
        // Arrange & Act
        var factory = new SecureTokenStorageFactory();
        var storage = factory.CreateStorage();

        // Assert
        storage.ShouldNotBeNull();
        storage.IsAvailable.ShouldBeTrue();
    }
}