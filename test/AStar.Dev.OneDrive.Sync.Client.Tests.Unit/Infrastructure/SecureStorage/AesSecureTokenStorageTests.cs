using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

public class AesSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    protected override ISecureTokenStorage CreateStorage() => new AesSecureTokenStorage();

    [Fact]
    public void IsAvailableAlwaysReturnsTrue()
    {
        ISecureTokenStorage storage = CreateStorage();
        storage.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void ReturnCorrectValueFromName()
    {
        ISecureTokenStorage storage = CreateStorage();
        storage.Name.ShouldBe("AES-256 Fallback");
    }

    [Fact]
    public async Task StoreAndRetrieve_VerifyEncryption_TokenNotStoredInPlaintext()
    {
        var storage = new AesSecureTokenStorage();
        const string key = "test-encryption-key";
        const string token = "sensitive-token-data";

        try
        {
            await storage.StoreTokenAsync(key, token);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var storageDirectory = Path.Combine(appData, "AStar", "OneDriveSync", "Tokens");
            var safeKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
                .Replace("/", "_")
                .Replace("+", "-");
            var filePath = Path.Combine(storageDirectory, $"{safeKey}.aes");
            File.Exists(filePath).ShouldBeTrue();
            var fileContent = await File.ReadAllBytesAsync(filePath);
            System.Text.Encoding.UTF8.GetString(fileContent).ShouldNotContain(token); // Token should be encrypted, not plaintext
        }
        finally
        {
            await storage.DeleteTokenAsync(key);
        }
    }

    [Fact]
    public async Task TwoStorageInstances_SameKey_CanRetrieveToken()
    {
        var storage1 = new AesSecureTokenStorage();
        var storage2 = new AesSecureTokenStorage();
        const string key = "test-shared-key";
        const string token = "shared-token";

        try
        {
            await storage1.StoreTokenAsync(key, token);

            var retrieved = await storage2.RetrieveTokenAsync(key);
            retrieved.ShouldBe(token);
        }
        finally
        {
            await storage1.DeleteTokenAsync(key);
        }
    }

    [Fact]
    public async Task Retrieve_CorruptedFile_ReturnsNull()
    {
        var storage = new AesSecureTokenStorage();
        const string key = "test-corrupted";

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var storageDirectory = Path.Combine(appData, "AStar", "OneDriveSync", "Tokens");
        Directory.CreateDirectory(storageDirectory);

        var safeKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
            .Replace("/", "_")
            .Replace("+", "-");
        var filePath = Path.Combine(storageDirectory, $"{safeKey}.aes");

        try
        {
            await File.WriteAllBytesAsync(filePath, System.Text.Encoding.UTF8.GetBytes("corrupted-data"));
            var retrieved = await storage.RetrieveTokenAsync(key);
            retrieved.ShouldBeNull();
        }
        finally
        {
            if(File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
