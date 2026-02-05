using AStar.Dev.OneDrive.Sync.Client.Common.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Common.Models;

public class DeltaTokenShould
{
    [Fact]
    public void ThrowArgumentExceptionWhenIdIsNull()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.Id = null!);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenIdIsEmpty()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.Id = string.Empty);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenIdIsWhitespace()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.Id = "   ");
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsNull()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.HashedAccountId = null!);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsEmpty()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.HashedAccountId = string.Empty);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsWhitespace()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.HashedAccountId = "   ");
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsNull()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.DriveName = null!);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsEmpty()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.DriveName = string.Empty);
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsWhitespace()
    {
        var deltaToken = new DeltaToken();

        Should.Throw<ArgumentException>(() => deltaToken.DriveName = "   ");
    }

    [Fact]
    public void AllowValidIdValue()
    {
        var deltaToken = new DeltaToken { Id = "token-123" };

        deltaToken.Id.ShouldBe("token-123");
    }

    [Fact]
    public void AllowValidHashedAccountIdValue()
    {
        var deltaToken = new DeltaToken { HashedAccountId = "hashed-account-456" };

        deltaToken.HashedAccountId.ShouldBe("hashed-account-456");
    }

    [Fact]
    public void AllowValidDriveNameValue()
    {
        var deltaToken = new DeltaToken { DriveName = "root" };

        deltaToken.DriveName.ShouldBe("root");
    }

    [Fact]
    public void AllowNullTokenValue()
    {
        var deltaToken = new DeltaToken
        {
            Id = "token-id",
            HashedAccountId = "hashed-account",
            DriveName = "root",
            Token = null
        };

        deltaToken.Token.ShouldBeNull();
    }

    [Fact]
    public void AllowValidTokenValue()
    {
        var deltaToken = new DeltaToken
        {
            Id = "token-id",
            HashedAccountId = "hashed-account",
            DriveName = "root",
            Token = "delta-token-xyz"
        };

        deltaToken.Token.ShouldBe("delta-token-xyz");
    }

    [Fact]
    public void AllowNullLastSyncAtValue()
    {
        var deltaToken = new DeltaToken
        {
            Id = "token-id",
            HashedAccountId = "hashed-account",
            DriveName = "root",
            LastSyncAt = null
        };

        deltaToken.LastSyncAt.ShouldBeNull();
    }

    [Fact]
    public void AllowValidLastSyncAtValue()
    {
        DateTime syncTime = DateTime.UtcNow;
        var deltaToken = new DeltaToken
        {
            Id = "token-id",
            HashedAccountId = "hashed-account",
            DriveName = "root",
            LastSyncAt = syncTime
        };

        deltaToken.LastSyncAt.ShouldBe(syncTime);
    }

    [Fact]
    public void CreateValidDeltaTokenWithAllProperties()
    {
        DateTime syncTime = DateTime.UtcNow;
        var deltaToken = new DeltaToken
        {
            Id = "token-789",
            HashedAccountId = "account-hash-abc",
            DriveName = "documents",
            Token = "opaque-delta-token",
            LastSyncAt = syncTime
        };

        deltaToken.Id.ShouldBe("token-789");
        deltaToken.HashedAccountId.ShouldBe("account-hash-abc");
        deltaToken.DriveName.ShouldBe("documents");
        deltaToken.Token.ShouldBe("opaque-delta-token");
        deltaToken.LastSyncAt.ShouldBe(syncTime);
    }
}
