namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Services;

using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

public class HashingServiceShould
{
    private readonly IHashingService _hashingService = new HashingService();

    [Fact]
    public async Task ReturnSameHashForSameEmailRegardlessOfCase()
    {
        const string lowercaseEmail = "user@example.com";
        const string uppercaseEmail = "USER@EXAMPLE.COM";
        const string mixedCaseEmail = "User@Example.Com";

        string lowercaseHash = await _hashingService.HashEmailAsync(lowercaseEmail);
        string uppercaseHash = await _hashingService.HashEmailAsync(uppercaseEmail);
        string mixedCaseHash = await _hashingService.HashEmailAsync(mixedCaseEmail);

        lowercaseHash.ShouldBe(uppercaseHash);
        lowercaseHash.ShouldBe(mixedCaseHash);
    }

    [Fact]
    public async Task ReturnDifferentHashForDifferentEmails()
    {
        const string email1 = "user1@example.com";
        const string email2 = "user2@example.com";

        string hash1 = await _hashingService.HashEmailAsync(email1);
        string hash2 = await _hashingService.HashEmailAsync(email2);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public async Task ReturnConsistentHashForEmail()
    {
        const string email = "user@example.com";

        string hash1 = await _hashingService.HashEmailAsync(email);
        string hash2 = await _hashingService.HashEmailAsync(email);

        // Assert - same input should produce same hash
        hash1.ShouldBe(hash2);
    }

    [Fact]
    public async Task ReturnHexEncodedHashForEmail()
    {
        const string email = "user@example.com";

        string hash = await _hashingService.HashEmailAsync(email);

        hash.Length.ShouldBe(64);
        hash.ShouldMatch(@"^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ReturnDifferentHashForSameAccountIdWithDifferentSalt()
    {
        const string accountId = "account123";
        long createdAtTicks1 = DateTime.UtcNow.Ticks;
        long createdAtTicks2 = DateTime.UtcNow.AddSeconds(1).Ticks;

        string hash1 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks1);
        string hash2 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks2);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public async Task ReturnConsistentHashForAccountId()
    {
        const string accountId = "account123";
        long createdAtTicks = DateTime.UtcNow.Ticks;

        string hash1 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks);
        string hash2 = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks);

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public async Task ReturnHexEncodedHashForAccountId()
    {
        const string accountId = "account123";
        long createdAtTicks = DateTime.UtcNow.Ticks;

        string hash = await _hashingService.HashAccountIdAsync(accountId, createdAtTicks);

        hash.Length.ShouldBe(64);
        hash.ShouldMatch(@"^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullEmail()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _hashingService.HashEmailAsync(null!));
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullAccountId()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _hashingService.HashAccountIdAsync(null!, 12345));
    }

    [Fact]
    public async Task ThrowArgumentExceptionForEmptyEmail()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _hashingService.HashEmailAsync(string.Empty));
        ex.ParamName.ShouldBe("email");
    }

    [Fact]
    public async Task ThrowArgumentExceptionForEmptyAccountId()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _hashingService.HashAccountIdAsync(string.Empty, 12345));
        ex.ParamName.ShouldBe("accountId");
    }

    [Fact]
    public async Task ThrowArgumentExceptionForWhitespaceEmail()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _hashingService.HashEmailAsync("   "));
        ex.ParamName.ShouldBe("email");
    }

    [Fact]
    public async Task ReturnDifferentHashForDifferentAccountIds()
    {
        const long createdAtTicks = 123456789;

        string hash1 = await _hashingService.HashAccountIdAsync("account1", createdAtTicks);
        string hash2 = await _hashingService.HashAccountIdAsync("account2", createdAtTicks);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public async Task ProduceDeterministicHashForEmailLookups()
    {
        const string email = "john.doe@company.com";

        var hashes = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            hashes.Add(await _hashingService.HashEmailAsync(email));
        }

        hashes.Distinct().Count().ShouldBe(1);
    }
}
