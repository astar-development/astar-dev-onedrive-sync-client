namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// Linux-specific secure token storage using Secret Service API (D-Bus).
/// This is a stub implementation for cross-platform compilation.
/// Full implementation requires libsecret or D-Bus interop.
/// </summary>
public class LinuxSecureTokenStorage : ISecureTokenStorage
{
    /// <inheritdoc/>
    public string Name => "Linux Secret Service";

    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsLinux();

    /// <inheritdoc/>
    public Task StoreTokenAsync(string key, string token)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        // TODO: Implement using libsecret or D-Bus interop
        // Example: secret-tool store --label="Label" key value
        throw new NotImplementedException("Linux Secret Service storage requires platform-specific implementation.");
    }

    /// <inheritdoc/>
    public Task<string?> RetrieveTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        // TODO: Implement using libsecret or D-Bus interop
        // Example: secret-tool lookup key
        throw new NotImplementedException("Linux Secret Service storage requires platform-specific implementation.");
    }

    /// <inheritdoc/>
    public Task DeleteTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        // TODO: Implement using libsecret or D-Bus interop
        // Example: secret-tool clear key
        throw new NotImplementedException("Linux Secret Service storage requires platform-specific implementation.");
    }
}