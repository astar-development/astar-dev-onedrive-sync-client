using Shouldly;
using Xunit;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

/// <summary>
/// Tests for AES-256 encrypted fallback storage.
/// </summary>
public class AesSecureTokenStorageTests : SecureTokenStorageTestsBase
{
    protected override ISecureTokenStorage CreateStorage()
    {
        return new AesSecureTokenStorage();
    }

    [Fact]
    public void IsAvailable_AlwaysReturnsTrue()
    {
        // Arrange
        var storage = CreateStorage();

        // Assert
        storage.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var storage = CreateStorage();

        // Assert
        storage.Name.ShouldBe("AES-256 Fallback");
    }

    [Fact]
    public async Task StoreAndRetrieve_VerifyEncryption_TokenNotStoredInPlaintext()
    {
        // Arrange
        var storage = new AesSecureTokenStorage();
        const string key = "test-encryption-key";
        const string token = "sensitive-token-data";

        try
        {
            // Act
            await storage.StoreTokenAsync(key, token);

            // Get the storage directory and file
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var storageDirectory = Path.Combine(appData, "AStar", "OneDriveSync", "Tokens");
            var safeKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
                .Replace("/", "_")
                .Replace("+", "-");
            var filePath = Path.Combine(storageDirectory, $"{safeKey}.aes");

            // Assert file exists and doesn't contain plaintext token
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
        // Arrange
        var storage1 = new AesSecureTokenStorage();
        var storage2 = new AesSecureTokenStorage();
        const string key = "test-shared-key";
        const string token = "shared-token";

        try
        {
            // Act - Store with first instance
            await storage1.StoreTokenAsync(key, token);

            // Retrieve with second instance
            var retrieved = await storage2.RetrieveTokenAsync(key);

            // Assert
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
        // Arrange
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
            // Create a corrupted file
            await File.WriteAllBytesAsync(filePath, System.Text.Encoding.UTF8.GetBytes("corrupted-data"));

            // Act
            var retrieved = await storage.RetrieveTokenAsync(key);

            // Assert
            retrieved.ShouldBeNull();
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}