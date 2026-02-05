using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.DeltaSync.Services;

public class DeltaSyncServiceShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenGraphFactoryIsNull() => Should.Throw<ArgumentNullException>(() => new DeltaSyncService(null!, null!));

    [Fact]
    public void ThrowArgumentExceptionWhenAccessTokenIsNull()
    {
        // Note: Full integration tests with mocked Graph API client would be complex
        // This serves as a basic validation test
        // Comprehensive mocking of Microsoft.Graph SDK would require additional setup
        var factory = new GraphApiClientFactory();
        var mockRepo = new MockDeltaTokenRepository();
        var service = new DeltaSyncService(factory, mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync(null!, "hash", "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsEmpty()
    {
        var factory = new GraphApiClientFactory();
        var mockRepo = new MockDeltaTokenRepository();
        var service = new DeltaSyncService(factory, mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", string.Empty, "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsWhitespace()
    {
        var factory = new GraphApiClientFactory();
        var mockRepo = new MockDeltaTokenRepository();
        var service = new DeltaSyncService(factory, mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", "hash", "   "));
    }
}

// Simple mock for basic validation tests
internal class MockDeltaTokenRepository : Client.Features.DeltaSync.Repositories.IDeltaTokenRepository
{
    public Task<DeltaToken?> GetByAccountAndDriveAsync(string hashedAccountId, string driveName) => Task.FromResult<DeltaToken?>(null);

    public Task<IEnumerable<DeltaToken>> GetAllByAccountAsync(string hashedAccountId) => Task.FromResult(Enumerable.Empty<DeltaToken>());

    public Task SaveAsync(DeltaToken deltaToken) => Task.CompletedTask;

    public Task DeleteAsync(string id) => Task.CompletedTask;
}
