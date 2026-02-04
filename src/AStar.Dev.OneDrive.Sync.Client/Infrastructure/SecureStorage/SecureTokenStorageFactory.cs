namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// Factory for creating platform-specific secure token storage implementations.
/// </summary>
public class SecureTokenStorageFactory
{
    private readonly bool _useFallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureTokenStorageFactory"/> class.
    /// </summary>
    /// <param name="useFallback">If true, always use AES fallback instead of platform-specific storage.</param>
    public SecureTokenStorageFactory(bool useFallback = false) => _useFallback = useFallback;

    /// <summary>
    /// Creates the appropriate secure token storage for the current platform.
    /// </summary>
    /// <returns>An instance of <see cref="ISecureTokenStorage"/> appropriate for the platform.</returns>
    public ISecureTokenStorage CreateStorage()
    {
        if(_useFallback)
        {
            return new AesSecureTokenStorage();
        }

        ISecureTokenStorage? storage = null;

        if(OperatingSystem.IsWindows())
        {
            storage = new WindowsSecureTokenStorage();
        }
        else if(OperatingSystem.IsMacOS())
        {
            storage = new MacOSSecureTokenStorage();

            if(!storage.IsAvailable)
            {
                storage = new AesSecureTokenStorage();
            }
        }
        else if(OperatingSystem.IsLinux())
        {
            storage = new LinuxSecureTokenStorage();

            if(!storage.IsAvailable)
            {
                storage = new AesSecureTokenStorage();
            }
        }

        return storage ?? new AesSecureTokenStorage();
    }

    /// <summary>
    /// Gets the name of the storage implementation that would be created for the current platform.
    /// </summary>
    /// <returns>The name of the storage implementation.</returns>
    public string GetStorageName() => CreateStorage().Name;
}
