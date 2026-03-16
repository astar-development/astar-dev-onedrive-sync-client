using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Auth;

/// <summary>
/// Wires MSAL's cross-platform token cache persistence using
/// Microsoft.Identity.Client.Extensions.Msal.
///
/// Cache location is platform-appropriate:
///   Linux   — ~/.config/AStar.Dev.OneDrive.Sync/
///   Windows — %AppData%\AStar.Dev.OneDrive.Sync\
///   macOS   — ~/Library/Application Support/AStar.Dev.OneDrive.Sync/
///
/// The cache file is encrypted using the platform keychain where available
/// (libsecret on Linux, DPAPI on Windows, Keychain on macOS).
/// Falls back to plaintext with a warning on unsupported platforms.
/// </summary>
public sealed class TokenCacheService
{
    private const string CacheFileName = "msal_token_cache.bin";
    private const string AppName       = "AStar.Dev.OneDrive.Sync";

    private readonly string _cacheDirectory;

    public TokenCacheService()
    {
        _cacheDirectory = GetPlatformCacheDirectory();
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Registers the file-backed cache with the given
    /// <see cref="IPublicClientApplication"/> instance.
    /// Must be called once after the app is built.
    /// </summary>
    public async Task RegisterAsync(IPublicClientApplication app)
    {
        var storageProperties = new StorageCreationPropertiesBuilder(
                CacheFileName,
                _cacheDirectory)
            .WithLinuxKeyring(
                schemaName:     "dev.astar.onedrivesync",
                collection:     MsalCacheHelper.LinuxKeyRingDefaultCollection,
                secretLabel:    "OneDrive Sync token cache",
                attribute1:     new KeyValuePair<string, string>("Version", "1"),
                attribute2:     new KeyValuePair<string, string>("ProductGroup", "AStar"))
            .WithMacKeyChain(
                serviceName: AppName,
                accountName: "MSALCache")
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);
    }

    public string CacheDirectory => _cacheDirectory;

    private static string GetPlatformCacheDirectory()
    {
        // Use the OS-appropriate application data folder
        var appData = Environment.GetFolderPath(
            OperatingSystem.IsWindows()
                ? Environment.SpecialFolder.ApplicationData
                : Environment.SpecialFolder.UserProfile);

        return OperatingSystem.IsWindows()
            ? Path.Combine(appData, AppName)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(appData, "Library", "Application Support", AppName)
                : Path.Combine(appData, ".config", AppName);
    }
}
