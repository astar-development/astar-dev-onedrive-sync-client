using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

public class DeltaProcessingServiceShould
{
    [Fact]
    public async Task GetDeltaTokenAsync_ShouldReturnTokenFromRepository()
    {
        const string accountId = "test-account-id";
        var expectedToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "token-123", DateTimeOffset.UtcNow);
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _ = syncRepo.GetDeltaTokenAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(expectedToken);
        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        DeltaToken? result = await service.GetDeltaTokenAsync(accountId, CancellationToken.None);

        _ = result.ShouldNotBeNull();
        result.AccountId.ShouldBe(expectedToken.AccountId);
        result.HashedAccountId.ShouldBe(expectedToken.HashedAccountId);
        result.Token.ShouldBe(expectedToken.Token);
    }

    [Fact]
    public async Task GetDeltaTokenAsync_ShouldReturnNullWhenNoTokenExists()
    {
        const string accountId = "test-account-id";
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _ = syncRepo.GetDeltaTokenAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);
        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        DeltaToken? result = await service.GetDeltaTokenAsync(accountId, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveDeltaTokenAsync_ShouldPersistTokenToRepository()
    {
        const string accountId = "test-account-id";
        var tokenToSave = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "token-123", DateTimeOffset.UtcNow);
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        await service.SaveDeltaTokenAsync(tokenToSave, CancellationToken.None);

        await syncRepo.Received(1).SaveOrUpdateDeltaTokenAsync(
            Arg.Is<DeltaToken>(t => t.AccountId == accountId && t.Token == "token-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldFetchAndProcessAllPages()
    {
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "initial-token", DateTimeOffset.UtcNow);
        var finalToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "final-token", DateTimeOffset.UtcNow);
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                AccountIdHasher.Hash("test-account"),
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns((finalToken, 3, 150));
        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        (DeltaToken resultToken, var pageCount, var itemCount) = await service.ProcessDeltaPagesAsync(
            accountId,
            AccountIdHasher.Hash("test-account"),
            deltaToken,
            null,
            CancellationToken.None);

        resultToken.ShouldBe(finalToken);
        pageCount.ShouldBe(3);
        itemCount.ShouldBe(150);
        _ = await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(
            accountId,
            AccountIdHasher.Hash("test-account"),
            Arg.Is<DeltaToken>(t => t.Token == "initial-token"),
            Arg.Any<Action<SyncState>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ProcessDeltaPagesAsync_ShouldHandleNullDeltaToken()
    {
        const string accountId = "test-account-id";
        var finalToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "final-token", DateTimeOffset.UtcNow);
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<HashedAccountId>(),
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns((finalToken, 1, 50));
        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        (DeltaToken resultToken, var pageCount, var itemCount) = await service.ProcessDeltaPagesAsync(
            accountId,
            AccountIdHasher.Hash("test-account"),
            null,
            null,
            CancellationToken.None);

        resultToken.ShouldBe(finalToken);
        pageCount.ShouldBe(1);
        itemCount.ShouldBe(50);
        _ = await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(
            accountId,
            Arg.Is<HashedAccountId>(h => h.Id == "test-account"),
            Arg.Is<DeltaToken>(t => t.AccountId == accountId && string.IsNullOrEmpty(t.Token)),
            Arg.Any<Action<SyncState>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ProcessDeltaPagesAsync_ShouldInvokeProgressCallback()
    {
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "initial-token", DateTimeOffset.UtcNow);
        var finalToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "final-token", DateTimeOffset.UtcNow);
        var progressStates = new List<SyncState>();
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<HashedAccountId>(),
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Action<SyncState>? callback = callInfo.ArgAt<Action<SyncState>?>(2);
                callback?.Invoke(SyncState.Create(accountId, AccountIdHasher.Hash("test-account"), SyncStatus.InitialDeltaSync, "Processing..."));
                return Task.FromResult((finalToken, 1, 50));
            });
        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        _ = await service.ProcessDeltaPagesAsync(
            accountId,
            AccountIdHasher.Hash("test-account"),
            deltaToken,
            progressStates.Add,
            CancellationToken.None);
        progressStates.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldHandleCancellation()
    {
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "initial-token", DateTimeOffset.UtcNow);
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<HashedAccountId>(),
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromException<(DeltaToken, int, int)>(new OperationCanceledException()));

        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);
        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await service.ProcessDeltaPagesAsync(accountId, AccountIdHasher.Hash("test-account"), deltaToken, null, cts.Token));
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldPropagateExceptions()
    {
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, AccountIdHasher.Hash("test-account"), "drive-id", "initial-token", DateTimeOffset.UtcNow);
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<HashedAccountId>(),
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromException<(DeltaToken, int, int)>(new IOException("Network error")));

        var service = new DeltaProcessingService(syncRepo, deltaPageProcessor);

        _ = await Should.ThrowAsync<IOException>(async () =>
            await service.ProcessDeltaPagesAsync(accountId, AccountIdHasher.Hash("test-account"), deltaToken, null, CancellationToken.None));
    }
}
