using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

public class DeltaProcessingServiceShould
{
    [Fact]
    public async Task GetDeltaTokenAsync_ShouldReturnTokenFromRepository()
    {
        // Arrange
        const string accountId = "test-account-id";
        var expectedToken = new DeltaToken(accountId, "drive-id", "token-123", DateTimeOffset.UtcNow);
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        _ = syncRepo.GetDeltaTokenAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(expectedToken);
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act
        DeltaToken? result = await service.GetDeltaTokenAsync(accountId, CancellationToken.None);
        
        // Assert
        result.ShouldNotBeNull();
        result.AccountId.ShouldBe(expectedToken.AccountId);
        result.Token.ShouldBe(expectedToken.Token);
    }

    [Fact]
    public async Task GetDeltaTokenAsync_ShouldReturnNullWhenNoTokenExists()
    {
        // Arrange
        const string accountId = "test-account-id";
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        _ = syncRepo.GetDeltaTokenAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act
        DeltaToken? result = await service.GetDeltaTokenAsync(accountId, CancellationToken.None);
        
        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveDeltaTokenAsync_ShouldPersistTokenToRepository()
    {
        // Arrange
        const string accountId = "test-account-id";
        var tokenToSave = new DeltaToken(accountId, "drive-id", "token-123", DateTimeOffset.UtcNow);
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act
        await service.SaveDeltaTokenAsync(tokenToSave, CancellationToken.None);
        
        // Assert
        await syncRepo.Received(1).SaveOrUpdateDeltaTokenAsync(
            Arg.Is<DeltaToken>(t => t.AccountId == accountId && t.Token == "token-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldFetchAndProcessAllPages()
    {
        // Arrange
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, "drive-id", "initial-token", DateTimeOffset.UtcNow);
        var finalToken = new DeltaToken(accountId, "drive-id", "final-token", DateTimeOffset.UtcNow);
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns((finalToken, 3, 150));
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act
        (DeltaToken resultToken, int pageCount, int itemCount) = await service.ProcessDeltaPagesAsync(
            accountId,
            deltaToken,
            null,
            CancellationToken.None);
        
        // Assert
        resultToken.ShouldBe(finalToken);
        pageCount.ShouldBe(3);
        itemCount.ShouldBe(150);
        
        await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(
            accountId,
            Arg.Is<DeltaToken>(t => t.Token == "initial-token"),
            Arg.Any<Action<SyncState>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldHandleNullDeltaToken()
    {
        // Arrange
        const string accountId = "test-account-id";
        var finalToken = new DeltaToken(accountId, "drive-id", "final-token", DateTimeOffset.UtcNow);
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        _ = deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns((finalToken, 1, 50));
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act
        (DeltaToken resultToken, int pageCount, int itemCount) = await service.ProcessDeltaPagesAsync(
            accountId,
            null,
            null,
            CancellationToken.None);
        
        // Assert
        resultToken.ShouldBe(finalToken);
        pageCount.ShouldBe(1);
        itemCount.ShouldBe(50);
        
        await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(
            accountId,
            Arg.Is<DeltaToken>(t => t.AccountId == accountId && string.IsNullOrEmpty(t.Token)),
            Arg.Any<Action<SyncState>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldInvokeProgressCallback()
    {
        // Arrange
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, "drive-id", "initial-token", DateTimeOffset.UtcNow);
        var finalToken = new DeltaToken(accountId, "drive-id", "final-token", DateTimeOffset.UtcNow);
        var progressStates = new List<SyncState>();
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var callback = callInfo.ArgAt<Action<SyncState>?>(2);
                callback?.Invoke(SyncState.Create(accountId, SyncStatus.InitialDeltaSync, "Processing..."));
                return Task.FromResult((finalToken, 1, 50));
            });
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act
        await service.ProcessDeltaPagesAsync(
            accountId,
            deltaToken,
            progressStates.Add,
            CancellationToken.None);
        
        // Assert
        progressStates.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldHandleCancellation()
    {
        // Arrange
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, "drive-id", "initial-token", DateTimeOffset.UtcNow);
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromException<(DeltaToken, int, int)>(new OperationCanceledException()));
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await service.ProcessDeltaPagesAsync(accountId, deltaToken, null, cts.Token));
    }

    [Fact]
    public async Task ProcessDeltaPagesAsync_ShouldPropagateExceptions()
    {
        // Arrange
        const string accountId = "test-account-id";
        var deltaToken = new DeltaToken(accountId, "drive-id", "initial-token", DateTimeOffset.UtcNow);
        
        ISyncRepository syncRepo = Substitute.For<ISyncRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        
        deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                Arg.Any<DeltaToken>(),
                Arg.Any<Action<SyncState>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromException<(DeltaToken, int, int)>(new IOException("Network error")));
        
        var service = new DeltaProcessingService(syncRepo, graphApiClient, deltaPageProcessor);
        
        // Act & Assert
        await Should.ThrowAsync<IOException>(async () =>
            await service.ProcessDeltaPagesAsync(accountId, deltaToken, null, CancellationToken.None));
    }
}
