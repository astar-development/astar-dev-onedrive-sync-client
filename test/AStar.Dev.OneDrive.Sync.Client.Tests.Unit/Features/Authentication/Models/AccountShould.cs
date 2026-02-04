using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Models;

public class AccountShould
{
    [Fact]
    public void CreateAccountWithValidProperties()
    {
        var accountId = Guid.NewGuid();
        const string hashedEmail = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        const string hashedAccountIdValue = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
        const string homeSyncDirectory = "/home/user/OneDrive";
        const int maxConcurrent = 5;
        const bool debugLoggingEnabled = false;
        const bool isAdmin = false;

        var account = new Account
        {
            Id = accountId,
            HashedEmail = hashedEmail,
            HashedAccountId = hashedAccountIdValue,
            HomeSyncDirectory = homeSyncDirectory,
            MaxConcurrent = maxConcurrent,
            DebugLoggingEnabled = debugLoggingEnabled,
            IsAdmin = isAdmin
        };

        account.Id.ShouldBe(accountId);
        account.HashedEmail.ShouldBe(hashedEmail);
        account.HashedAccountId.ShouldBe(hashedAccountIdValue);
        account.HomeSyncDirectory.ShouldBe(homeSyncDirectory);
        account.MaxConcurrent.ShouldBe(maxConcurrent);
        account.DebugLoggingEnabled.ShouldBeFalse();
        account.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void ThrowArgumentExceptionForNullHashedEmail()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new Account { HashedEmail = null! }; });
        ex.Message.ShouldContain("HashedEmail");
    }

    [Fact]
    public void ThrowArgumentExceptionForEmptyHashedEmail()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new Account { HashedEmail = string.Empty }; });
        ex.Message.ShouldContain("HashedEmail");
    }

    [Fact]
    public void ThrowArgumentExceptionForWhitespaceHashedEmail()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new Account { HashedEmail = "   " }; });
        ex.Message.ShouldContain("HashedEmail");
    }

    [Fact]
    public void ThrowArgumentExceptionForNullHashedAccountId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new Account { HashedAccountId = null! }; });
        ex.Message.ShouldContain("HashedAccountId");
    }

    [Fact]
    public void ThrowArgumentExceptionForEmptyHashedAccountId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new Account { HashedAccountId = string.Empty }; });
        ex.Message.ShouldContain("HashedAccountId");
    }

    [Fact]
    public void ThrowArgumentExceptionForWhitespaceHashedAccountId()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => { var _ = new Account { HashedAccountId = "   " }; });
        ex.Message.ShouldContain("HashedAccountId");
    }

    [Fact]
    public void AllowEmptyHomeSyncDirectory()
    {
        var account = new Account
        {
            HomeSyncDirectory = string.Empty
        };

        account.HomeSyncDirectory.ShouldBe(string.Empty);
    }

    [Fact]
    public void AllowNullHomeSyncDirectory()
    {
        var account = new Account
        {
            HomeSyncDirectory = null
        };

        account.HomeSyncDirectory.ShouldBeNull();
    }

    [Fact]
    public void AllowAnyStringForHomeSyncDirectory()
    {
        const string invalidPath = "this/is/not/valid/but/allowed";

        var account = new Account
        {
            HomeSyncDirectory = invalidPath
        };

        account.HomeSyncDirectory.ShouldBe(invalidPath);
    }

    [Fact]
    public void HaveDefaultMaxConcurrentAndFlags()
    {
        var account = new Account();

        account.MaxConcurrent.ShouldBe(5);
        account.DebugLoggingEnabled.ShouldBeFalse();
        account.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void TrackCreatedAtTimestamp()
    {
        DateTime beforeCreation = DateTime.UtcNow;

        var account = new Account();
        DateTime afterCreation = DateTime.UtcNow;

        account.CreatedAt.ShouldNotBe(default);
        account.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        account.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
    }

    [Fact]
    public void TrackUpdatedAtTimestamp()
    {
        var account = new Account();

        account.UpdatedAt.ShouldNotBe(default);
        account.UpdatedAt.ShouldBeGreaterThanOrEqualTo(account.CreatedAt);
    }

    [Fact]
    public void InitializeNavigationCollectionsAsEmpty()
    {
        var account = new Account();

        account.FileSystemItems.ShouldNotBeNull();
        account.FileSystemItems.ShouldBeEmpty();
        account.DeltaTokens.ShouldNotBeNull();
        account.DeltaTokens.ShouldBeEmpty();
        account.ConflictLogs.ShouldNotBeNull();
        account.ConflictLogs.ShouldBeEmpty();
        account.ApplicationLogs.ShouldNotBeNull();
        account.ApplicationLogs.ShouldBeEmpty();
    }

    [Fact]
    public void AllowCustomMaxConcurrentValue()
    {
        var account = new Account { MaxConcurrent = 10 };

        account.MaxConcurrent.ShouldBe(10);
    }

    [Fact]
    public void AllowToggleDebugLogging()
    {
        var account = new Account { DebugLoggingEnabled = true };

        account.DebugLoggingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AllowToggleAdminFlag()
    {
        var account = new Account { IsAdmin = true };

        account.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public void GenerateUniqueIdOnCreation()
    {
        var account1 = new Account();
        var account2 = new Account();

        account1.Id.ShouldNotBe(Guid.Empty);
        account2.Id.ShouldNotBe(Guid.Empty);
        account1.Id.ShouldNotBe(account2.Id);
    }
}
