using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.SecureStorage;

/// <summary>
/// Base test class for secure token storage implementations.
/// Provides common test scenarios that all implementations must pass.
/// </summary>
public abstract class SecureTokenStorageTestsBase
{
    protected abstract AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage.ISecureTokenStorage CreateStorage();

    [Fact]
    public async Task StoreAndRetrieve_ValidToken_ReturnsToken()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "test-key-1";
        const string token = "test-token-value-123";

        try
        {
            await storage.StoreTokenAsync(key, token);
            var retrieved = await storage.RetrieveTokenAsync(key);
            retrieved.ShouldNotBeNull();
            retrieved.ShouldBe(token);
        }
        finally
        {
            await storage.DeleteTokenAsync(key);
        }
    }

    [Fact]
    public async Task Retrieve_NonExistentKey_ReturnsNull()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "non-existent-key";
        var retrieved = await storage.RetrieveTokenAsync(key);
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_ExistingToken_RemovesToken()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "test-key-delete";
        const string token = "token-to-delete";

        await storage.StoreTokenAsync(key, token);
        await storage.DeleteTokenAsync(key);
        var retrieved = await storage.RetrieveTokenAsync(key);
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_NonExistentToken_DoesNotThrow()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "non-existent-delete-key";
        await storage.DeleteTokenAsync(key);
    }

    [Fact]
    public async Task StoreAndRetrieve_SpecialCharacters_HandlesCorrectly()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "test-special-chars";
        const string token = "token!@#$%^&*()_+-=[]{}|;:',.<>?/~`";

        try
        {
            await storage.StoreTokenAsync(key, token);
            var retrieved = await storage.RetrieveTokenAsync(key);
            retrieved.ShouldNotBeNull();
            retrieved.ShouldBe(token);
        }
        finally
        {
            await storage.DeleteTokenAsync(key);
        }
    }

    [Fact]
    public async Task StoreAndRetrieve_LongToken_HandlesCorrectly()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "test-long-token";
        var token = new string('A', 10000); // 10KB token

        try
        {
            await storage.StoreTokenAsync(key, token);
            var retrieved = await storage.RetrieveTokenAsync(key);
            retrieved.ShouldNotBeNull();
            retrieved.ShouldBe(token);
        }
        finally
        {
            await storage.DeleteTokenAsync(key);
        }
    }

    [Fact]
    public async Task Store_OverwriteExisting_UpdatesToken()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key = "test-overwrite";
        const string token1 = "original-token";
        const string token2 = "updated-token";

        try
        {
            await storage.StoreTokenAsync(key, token1);
            await storage.StoreTokenAsync(key, token2);
            var retrieved = await storage.RetrieveTokenAsync(key);
            retrieved.ShouldNotBeNull();
            retrieved.ShouldBe(token2);
        }
        finally
        {
            await storage.DeleteTokenAsync(key);
        }
    }

    [Fact]
    public async Task StoreAndRetrieve_MultipleKeys_IsolatesValues()
    {
        var storage = CreateStorage();
        if (!storage.IsAvailable)
        {
            return;
        }

        const string key1 = "test-multi-1";
        const string key2 = "test-multi-2";
        const string token1 = "token-1";
        const string token2 = "token-2";

        try
        {
            await storage.StoreTokenAsync(key1, token1);
            await storage.StoreTokenAsync(key2, token2);

            var retrieved1 = await storage.RetrieveTokenAsync(key1);
            var retrieved2 = await storage.RetrieveTokenAsync(key2);
            retrieved1.ShouldBe(token1);
            retrieved2.ShouldBe(token2);
        }
        finally
        {
            await storage.DeleteTokenAsync(key1);
            await storage.DeleteTokenAsync(key2);
        }
    }

    [Fact]
    public void Name_ShouldNotBeNullOrEmpty()
    {
        var storage = CreateStorage();
        storage.Name.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void IsAvailable_ShouldReturnBool()
    {
        var storage = CreateStorage();
        storage.IsAvailable.ShouldBeOfType<bool>();
    }
}