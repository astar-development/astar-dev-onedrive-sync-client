using System.Security.Cryptography;
using System.Text;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

/// <summary>
/// Cross-platform fallback secure token storage using AES-256 encryption.
/// Uses a machine-specific key derived from hardware identifiers.
/// WARNING: This is less secure than platform-specific storage but provides a fallback.
/// </summary>
public class AesSecureTokenStorage : ISecureTokenStorage
{
    private readonly string _storageDirectory;
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesSecureTokenStorage"/> class.
    /// </summary>
    public AesSecureTokenStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(appData, "AStar", "OneDriveSync", "Tokens");

        if(!Directory.Exists(_storageDirectory))
        {
            _ = Directory.CreateDirectory(_storageDirectory);
        }

        _key = DeriveKey();
    }

    /// <inheritdoc/>
    public string Name => "AES-256 Fallback";

    /// <inheritdoc/>
    public bool IsAvailable => true; // Always available as fallback

    /// <inheritdoc/>
    public async Task StoreTokenAsync(string key, string token)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        var filePath = GetFilePath(key);
        await File.WriteAllBytesAsync(filePath, result);
    }

    /// <inheritdoc/>
    public async Task<string?> RetrieveTokenAsync(string key)
    {
        var filePath = GetFilePath(key);

        if(!File.Exists(filePath))
            return null;

        try
        {
            var data = await File.ReadAllBytesAsync(filePath);

            using var aes = Aes.Create();
            aes.Key = _key;

            if(data.Length < aes.IV.Length)
                return null;

            var iv = new byte[aes.IV.Length];
            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            aes.IV = iv;

            var encryptedBytes = new byte[data.Length - iv.Length];
            Buffer.BlockCopy(data, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            var tokenBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(tokenBytes);
        }
        catch(CryptographicException)
        {
            return null;
        }
        catch(ArgumentException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public Task DeleteTokenAsync(string key)
    {
        var filePath = GetFilePath(key);

        if(File.Exists(filePath))
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

        return Path.Combine(_storageDirectory, $"{safeKey}.aes");
    }

    private static byte[] DeriveKey() => SHA256.HashData(Encoding.UTF8.GetBytes(Environment.MachineName + Environment.UserName));
}
