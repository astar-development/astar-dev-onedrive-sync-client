using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.DeltaSync.Repositories;

public class DeltaTokenRepositoryShould : IDisposable
{
    private readonly OneDriveSyncDbContext _context;
    private readonly IDeltaTokenRepository _repository;

    public DeltaTokenRepositoryShould()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OneDriveSyncDbContext(options);
        _repository = new DeltaTokenRepository(_context);
    }

    [Fact]
    public async Task SaveNewDeltaTokenAndPersistToDatabase()
    {
        var deltaToken = new DeltaToken
        {
            Id = "token-123",
            HashedAccountId = "account-hash",
            DriveName = "root",
            Token = "delta-xyz",
            LastSyncAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(deltaToken);

        DeltaToken? saved = await _context.DeltaTokens.FindAsync(deltaToken.Id);
        saved.ShouldNotBeNull();
        saved.Id.ShouldBe(deltaToken.Id);
        saved.Token.ShouldBe(deltaToken.Token);
    }

    [Fact]
    public async Task GetByAccountAndDriveReturnsTokenWhenExists()
    {
        var deltaToken = new DeltaToken
        {
            Id = "token-456",
            HashedAccountId = "account-abc",
            DriveName = "documents",
            Token = "delta-token-123"
        };

        await _context.DeltaTokens.AddAsync(deltaToken);
        await _context.SaveChangesAsync();

        DeltaToken? result = await _repository.GetByAccountAndDriveAsync("account-abc", "documents");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(deltaToken.Id);
        result.DriveName.ShouldBe("documents");
    }

    [Fact]
    public async Task GetByAccountAndDriveReturnsNullWhenNotExists()
    {
        DeltaToken? result = await _repository.GetByAccountAndDriveAsync("non-existent", "root");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllByAccountReturnsAllTokensForAccount()
    {
        const string hashedAccountId = "account-xyz";

        var token1 = new DeltaToken { Id = "token-1", HashedAccountId = hashedAccountId, DriveName = "root", Token = "delta-1" };
        var token2 = new DeltaToken { Id = "token-2", HashedAccountId = hashedAccountId, DriveName = "documents", Token = "delta-2" };
        var token3 = new DeltaToken { Id = "token-3", HashedAccountId = "other-account", DriveName = "root", Token = "delta-3" };

        await _context.DeltaTokens.AddRangeAsync(token1, token2, token3);
        await _context.SaveChangesAsync();

        IEnumerable<DeltaToken> results = await _repository.GetAllByAccountAsync(hashedAccountId);

        results.Count().ShouldBe(2);
        results.All(dt => dt.HashedAccountId == hashedAccountId).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveUpdatesExistingDeltaToken()
    {
        var originalToken = new DeltaToken
        {
            Id = "token-update",
            HashedAccountId = "account-update",
            DriveName = "root",
            Token = "original-token",
            LastSyncAt = DateTime.UtcNow.AddHours(-1)
        };

        await _context.DeltaTokens.AddAsync(originalToken);
        await _context.SaveChangesAsync();

        DateTime newSyncTime = DateTime.UtcNow;
        var updatedToken = new DeltaToken
        {
            Id = "token-update",
            HashedAccountId = "account-update",
            DriveName = "root",
            Token = "updated-token",
            LastSyncAt = newSyncTime
        };

        await _repository.SaveAsync(updatedToken);

        DeltaToken? saved = await _context.DeltaTokens.FindAsync("token-update");
        saved.ShouldNotBeNull();
        saved.Token.ShouldBe("updated-token");
        saved.LastSyncAt.ShouldBe(newSyncTime);
    }

    [Fact]
    public async Task DeleteRemovesDeltaTokenFromDatabase()
    {
        var deltaToken = new DeltaToken
        {
            Id = "token-delete",
            HashedAccountId = "account-delete",
            DriveName = "root",
            Token = "delete-me"
        };

        await _context.DeltaTokens.AddAsync(deltaToken);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(deltaToken.Id);

        DeltaToken? deleted = await _context.DeltaTokens.FindAsync(deltaToken.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteDoesNothingWhenTokenNotFound()
    {
        await _repository.DeleteAsync("non-existent-id");

        int count = await _context.DeltaTokens.CountAsync();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveHandlesMultipleDrivesPerAccount()
    {
        const string hashedAccountId = "multi-drive-account";

        var rootToken = new DeltaToken
        {
            Id = "token-root",
            HashedAccountId = hashedAccountId,
            DriveName = "root",
            Token = "root-delta"
        };

        var docsToken = new DeltaToken
        {
            Id = "token-docs",
            HashedAccountId = hashedAccountId,
            DriveName = "documents",
            Token = "docs-delta"
        };

        await _repository.SaveAsync(rootToken);
        await _repository.SaveAsync(docsToken);

        IEnumerable<DeltaToken> results = await _repository.GetAllByAccountAsync(hashedAccountId);
        results.Count().ShouldBe(2);
        results.Any(dt => dt.DriveName == "root").ShouldBeTrue();
        results.Any(dt => dt.DriveName == "documents").ShouldBeTrue();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
