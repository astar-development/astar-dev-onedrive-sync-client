using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// Windows-specific secure token storage using DPAPI (Data Protection API).
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSecureTokenStorage : ISecureTokenStorage
{
    private readonly string _storageDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsSecureTokenStorage"/> class.
    /// </summary>
    public WindowsSecureTokenStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(appData, "AStar", "OneDriveSync", "Tokens");
        
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    /// <inheritdoc/>
    public string Name => "Windows DPAPI";

    /// <inheritdoc/>
    public bool IsAvailable => OperatingSystem.IsWindows();

    /// <inheritdoc/>
    public async Task StoreTokenAsync(string key, string token)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows.");

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = ProtectedData.Protect(tokenBytes, null, DataProtectionScope.CurrentUser);
        
        var filePath = GetFilePath(key);
        await File.WriteAllBytesAsync(filePath, encryptedBytes);
    }

    /// <inheritdoc/>
    public async Task<string?> RetrieveTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows.");

        var filePath = GetFilePath(key);
        
        if (!File.Exists(filePath))
            return null;

        try
        {
            var encryptedBytes = await File.ReadAllBytesAsync(filePath);
            var tokenBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(tokenBytes);
        }
        catch (CryptographicException)
        {
            // Token was encrypted by a different user or is corrupted
            return null;
        }
    }

    /// <inheritdoc/>
    public Task DeleteTokenAsync(string key)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows.");

        var filePath = GetFilePath(key);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(string key)
    {
        var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace("/", "_")
            .Replace("+", "-");
        return Path.Combine(_storageDirectory, $"{safeKey}.dat");
    }
}