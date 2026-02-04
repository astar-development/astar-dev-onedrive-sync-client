namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Services;

using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Unit tests for HashingService.
/// Tests SHA256 hashing for email and account ID with proper case-insensitivity and salt handling.
/// </summary>
public class HashingServiceShould
{
    private readonly IHashingService _hashingService = new HashingService();

    [Fact]
    public async Task ReturnSameHashForSameEmailRegardlessOfCase()
    {
        // Arrange
        const string lowercaseEmail = "user@example.com";
        const string uppercaseEmail = "USER@EXAMPLE.COM";
        const string mixedCaseEmail = "User@Example.Com";

        // Act
        string lowercaseHash = await _hashingService.HashEmailAsync(lowercaseEmail);
        string uppercaseHash = await _hashingService.HashEmailAsync(uppercaseEmail);
        string mixedCaseHash = await _hashingService.HashEmailAsync(mixedCaseEmail);

        // Assert
        lowercaseHash.ShouldBe(uppercaseHash);
        lowercaseHash.ShouldBe(mixedCaseHash);
    }

    [Fact]
    public async Task ReturnDifferentHashForDifferentEmails()
    {
        // Arrange
        const string email1 = "user1@example.com";
        const string email2 = "user2@example.com";

        // Act
        string hash1 = await _hashingService.HashEmailAsync(email1);
        string hash2 = await _hashingService.HashEmailAsync(email2);

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public async Task ReturnConsistentHashForEmail()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        string hash1 = await _hashingService.HashEmailAsync(email);
        string hash2 = await _hashingService.HashEmailAsync(email);

        // Assert - same input should produce same hash
        hash1.ShouldBe(hash2);
    }

    [Fact]
    public async Task ReturnHexEncodedHashForEmail()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        string hash = await _hashingService.HashEmailAsync(email);

        // Assert - SHA256 produces 64 hex characters
        hash.Length.ShouldBe(64);
        hash.ShouldMatch(@"^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ReturnDifferentHashForSameAccountIdWithDifferentSalt()
    {
        // Arrange
        const string accountId = "account123";
        long createdAtTicks1 = DateTime.UtcNow.Ticks;
        long createdAtTicks2 = DateTime.UtcNow.AddSeconds(1).Ticks;

        // Act
        string hash1 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks1);
        string hash2 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks2);

        // Assert - different salts should produce different hashes
        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public async Task ReturnConsistentHashForAccountId()
    {
        // Arrange
        const string accountId = "account123";
        long createdAtTicks = DateTime.UtcNow.Ticks;

        // Act
        string hash1 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks);
        string hash2 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks);

        // Assert - same input and salt should produce same hash
        hash1.ShouldBe(hash2);
    }

    [Fact]
    public async Task ReturnHexEncodedHashForAccountId()
    {
        // Arrange
        const string accountId = "account123";
        long createdAtTicks = DateTime.UtcNow.Ticks;

        // Act
        string hash = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks);

        // Assert - SHA256 produces 64 hex characters
        hash.Length.ShouldBe(64);
        hash.ShouldMatch(@"^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullEmail()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _hashingService.HashEmailAsync(null!));
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullAccountId()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _hashingService.HashAccountIdAsync(null!, 12345));
    }

    [Fact]
    public async Task ThrowArgumentExceptionForEmptyEmail()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _hashingService.HashEmailAsync(string.Empty));
        ex.ParamName.ShouldBe("email");
    }

    [Fact]
    public async Task ThrowArgumentExceptionForEmptyAccountId()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _hashingService.HashAccountIdAsync(string.Empty, 12345));
        ex.ParamName.ShouldBe("accountId");
    }

    [Fact]
    public async Task ThrowArgumentExceptionForWhitespaceEmail()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _hashingService.HashEmailAsync("   "));
        ex.ParamName.ShouldBe("email");
    }

    [Fact]
    public async Task ReturnDifferentHashForDifferentAccountIds()
    {
        // Arrange
        const long createdAtTicks = 123456789;

        // Act
        string hash1 = await _hashingService.HashAccountIdAsync("account1", createdAtTicks);
        string hash2 = await _hashingService.HashAccountIdAsync("account2", createdAtTicks);

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    /// <summary>
    /// Tests that email hashing is deterministic and suitable for lookups.
    /// This is important for account retrieval by hashed email.
    /// </summary>
    [Fact]
    public async Task ProduceDeterministicHashForEmailLookups()
    {
        // Arrange
        const string email = "john.doe@company.com";

        // Act - Hash the same email multiple times
        var hashes = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            hashes.Add(await _hashingService.HashEmailAsync(email));
        }

        // Assert - All hashes should be identical
        hashes.Distinct().Count().ShouldBe(1);
    }
}
