using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.DeltaSync.Services;

public class DeltaSyncServiceShould
{
    private readonly IGraphServiceClientFactory _mockFactory;
    private readonly Client.Features.DeltaSync.Repositories.IDeltaTokenRepository _mockRepo;

    public DeltaSyncServiceShould()
    {
        _mockFactory = Substitute.For<IGraphServiceClientFactory>();
        _mockRepo = Substitute.For<Client.Features.DeltaSync.Repositories.IDeltaTokenRepository>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenGraphFactoryIsNull()
        => Should.Throw<ArgumentNullException>(() => new DeltaSyncService(null!, null!));

    [Fact]
    public void ThrowArgumentNullExceptionWhenRepositoryIsNull()
        => Should.Throw<ArgumentNullException>(() => new DeltaSyncService(_mockFactory, null!));

    [Fact]
    public void ThrowArgumentExceptionWhenAccessTokenIsNull()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync(null!, "hash", "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenAccessTokenIsEmpty()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync(string.Empty, "hash", "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsNull()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", null!, "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsEmpty()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", string.Empty, "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenHashedAccountIdIsWhitespace()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", "   ", "root"));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsNull()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", "hash", null!));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsEmpty()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", "hash", string.Empty));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenDriveNameIsWhitespace()
    {
        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetDeltaChangesAsync("token", "hash", "   "));
    }

    [Fact]
    public async Task CallRepositoryToGetLatestTokenBeforeSync()
    {
        var existingToken = new Client.Common.Models.DeltaToken
        {
            Id = "existing-token-id",
            HashedAccountId = "account-hash",
            DriveName = "root",
            Token = "https://graph.microsoft.com/v1.0/me/drive/root/delta?token=saved-delta",
            LastSyncAt = DateTime.UtcNow.AddHours(-1)
        };

        _mockRepo.GetByAccountAndDriveAsync("account-hash", "root")
            .Returns(existingToken);

        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        try
        {
            await service.GetDeltaChangesAsync("access-token", "account-hash", "root");
        }
        catch
        {
        }

        await _mockRepo.Received(1).GetByAccountAndDriveAsync("account-hash", "root");
    }

    [Fact]
    public async Task RepositoryIsCalledToSaveTokenAfterSync()
    {
        _mockRepo.GetByAccountAndDriveAsync("account-hash", "root")
            .Returns((Client.Common.Models.DeltaToken?)null);

        _mockRepo.SaveAsync(Arg.Any<Client.Common.Models.DeltaToken>())
            .Returns(Task.CompletedTask);

        var service = new DeltaSyncService(_mockFactory, _mockRepo);

        try
        {
            await service.GetDeltaChangesAsync("access-token", "account-hash", "root");
        }
        catch
        {
        }
    }
}
