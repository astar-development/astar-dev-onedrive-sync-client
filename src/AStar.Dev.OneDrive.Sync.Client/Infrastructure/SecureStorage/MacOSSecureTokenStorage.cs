namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// macOS-specific secure token storage using Keychain.
/// This is a stub implementation for cross-platform compilation.
/// Full implementation requires platform-specific interop with Security framework.
/// </summary>
public class MacOSSecureTokenStorage : ISecureTokenStorage
{
    /// <inheritdoc/>
    public string Name => "macOS Keychain";

    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsMacOS();

    /// <inheritdoc/>
    public Task StoreTokenAsync(string key, string token)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");

        // TODO: Implement using Security framework interop
        // Example: security add-generic-password -a "user" -s "service" -w "password"
        throw new NotImplementedException("macOS Keychain storage requires platform-specific implementation.");
    }

    /// <inheritdoc/>
    public Task<string?> RetrieveTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");

        // TODO: Implement using Security framework interop
        // Example: security find-generic-password -a "user" -s "service" -w
        throw new NotImplementedException("macOS Keychain storage requires platform-specific implementation.");
    }

    /// <inheritdoc/>
    public Task DeleteTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("macOS Keychain is only available on macOS.");

        // TODO: Implement using Security framework interop
        // Example: security delete-generic-password -a "user" -s "service"
        throw new NotImplementedException("macOS Keychain storage requires platform-specific implementation.");
    }
}