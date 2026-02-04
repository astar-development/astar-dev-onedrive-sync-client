namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// Interface for platform-specific secure token storage.
/// </summary>
public interface ISecureTokenStorage
{
    /// <summary>
    /// Stores a token securely using platform-specific mechanisms.
    /// </summary>
    /// <param name="key">The key to identify the token.</param>
    /// <param name="token">The token value to store securely.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreTokenAsync(string key, string token);

    /// <summary>
    /// Retrieves a token from secure storage.
    /// </summary>
    /// <param name="key">The key to identify the token.</param>
    /// <returns>The token value, or null if not found.</returns>
    Task<string?> RetrieveTokenAsync(string key);

    /// <summary>
    /// Deletes a token from secure storage.
    /// </summary>
    /// <param name="key">The key to identify the token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteTokenAsync(string key);

    /// <summary>
    /// Gets a value indicating whether this storage implementation is available on the current platform.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the name of the storage implementation.
    /// </summary>
    string Name { get; }
}