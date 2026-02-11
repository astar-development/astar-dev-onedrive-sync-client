using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

public class SyncStateCoordinatorShould
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory;

    public SyncStateCoordinatorShould() => _contextFactory = new PooledDbContextFactory<SyncDbContext>(
            new DbContextOptionsBuilder<SyncDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task InitializeSessionWithDetailedLoggingEnabled()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? sessionId = await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, CancellationToken.None);

        sessionId.ShouldNotBeNull();
        sessionId.ShouldNotBeEmpty();

        using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncSessionLogEntity? session = await context.SyncSessionLogs.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        _ = session.ShouldNotBeNull();
        session.AccountId.ShouldBe("test-account");
        ((SyncStatus)session.Status).ShouldBe(SyncStatus.Running);
        session.CompletedUtc.ShouldBeNull();
    }

    [Fact]
    public async Task InitializeSessionWithDetailedLoggingDisabled()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? sessionId = await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: false, CancellationToken.None);

        sessionId.ShouldBeNull();

        using SyncDbContext context = _contextFactory.CreateDbContext();
        List<SyncSessionLogEntity> sessions = await context.SyncSessionLogs.ToListAsync(TestContext.Current.CancellationToken);
        sessions.Count.ShouldBe(0);
    }

    [Fact]
    public void UpdateProgressShouldPublishStateToObservers()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        SyncState? receivedState = null;
        coordinator.Progress.Subscribe(state => receivedState = state);

        coordinator.UpdateProgress("test-account", SyncStatus.Running, totalFiles: 10, completedFiles: 5, totalBytes: 1000, completedBytes: 500);

        receivedState.ShouldNotBeNull();
        receivedState.AccountId.ShouldBe("test-account");
        receivedState.Status.ShouldBe(SyncStatus.Running);
        receivedState.TotalFiles.ShouldBe(10);
        receivedState.CompletedFiles.ShouldBe(5);
        receivedState.TotalBytes.ShouldBe(1000);
        receivedState.CompletedBytes.ShouldBe(500);
    }

    [Fact]
    public void UpdateProgressShouldCalculateSpeed()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        SyncState? receivedState = null;
        coordinator.Progress.Subscribe(state => receivedState = state);

        coordinator.UpdateProgress("test-account", SyncStatus.Running, completedBytes: 0);
        Thread.Sleep(200);
        coordinator.UpdateProgress("test-account", SyncStatus.Running, completedBytes: 10_485_760); // 10 MB

        receivedState.ShouldNotBeNull();
        receivedState.MegabytesPerSecond.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void UpdateProgressShouldCalculateEstimatedTimeRemaining()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        SyncState? receivedState = null;
        coordinator.Progress.Subscribe(state => receivedState = state);

        coordinator.UpdateProgress("test-account", SyncStatus.Running, totalBytes: 100_000_000, completedBytes: 0);
        Thread.Sleep(200);
        coordinator.UpdateProgress("test-account", SyncStatus.Running, totalBytes: 100_000_000, completedBytes: 10_485_760);

        receivedState.ShouldNotBeNull();
        receivedState.EstimatedSecondsRemaining.ShouldNotBeNull();
        receivedState.EstimatedSecondsRemaining.Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task RecordCompletionShouldUpdateSessionLog()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? sessionId = await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, CancellationToken.None);
        sessionId.ShouldNotBeNull();

        await coordinator.RecordCompletionAsync(uploadCount: 5, downloadCount: 3, deleteCount: 1, conflictCount: 2, completedBytes: 1024, CancellationToken.None);

        using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncSessionLogEntity? session = await context.SyncSessionLogs.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        _ = session.ShouldNotBeNull();
        ((SyncStatus)session.Status).ShouldBe(SyncStatus.Completed);
        session.FilesUploaded.ShouldBe(5);
        session.FilesDownloaded.ShouldBe(3);
        session.FilesDeleted.ShouldBe(1);
        session.ConflictsDetected.ShouldBe(2);
        session.TotalBytes.ShouldBe(1024);
        session.CompletedUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecordCompletionShouldDoNothingWhenNoSessionActive()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        await coordinator.RecordCompletionAsync(uploadCount: 5, downloadCount: 3, deleteCount: 1, conflictCount: 2, completedBytes: 1024, CancellationToken.None);

        using SyncDbContext context = _contextFactory.CreateDbContext();
        List<SyncSessionLogEntity> sessions = await context.SyncSessionLogs.ToListAsync(TestContext.Current.CancellationToken);
        sessions.Count.ShouldBe(0);
    }

    [Fact]
    public async Task RecordFailureShouldUpdateSessionLog()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? sessionId = await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, CancellationToken.None);
        sessionId.ShouldNotBeNull();

        await coordinator.RecordFailureAsync(CancellationToken.None);

        using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncSessionLogEntity? session = await context.SyncSessionLogs.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        _ = session.ShouldNotBeNull();
        ((SyncStatus)session.Status).ShouldBe(SyncStatus.Failed);
        session.CompletedUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task RecordCancellationShouldUpdateSessionLog()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? sessionId = await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, CancellationToken.None);
        sessionId.ShouldNotBeNull();

        await coordinator.RecordCancellationAsync(CancellationToken.None);

        using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncSessionLogEntity? session = await context.SyncSessionLogs.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        _ = session.ShouldNotBeNull();
        ((SyncStatus)session.Status).ShouldBe(SyncStatus.Paused);
        session.CompletedUtc.ShouldNotBeNull();
    }

    [Fact]
    public void GetCurrentStateShouldReturnLastPublishedState()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        coordinator.UpdateProgress("test-account", SyncStatus.Running, totalFiles: 100, completedFiles: 50);

        SyncState currentState = coordinator.GetCurrentState();

        currentState.AccountId.ShouldBe("test-account");
        currentState.Status.ShouldBe(SyncStatus.Running);
        currentState.TotalFiles.ShouldBe(100);
        currentState.CompletedFiles.ShouldBe(50);
    }

    [Fact]
    public async Task GetCurrentSessionIdShouldReturnActiveSessionId()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? sessionId = await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, CancellationToken.None);

        string? currentSessionId = coordinator.GetCurrentSessionId();

        currentSessionId.ShouldBe(sessionId);
    }

    [Fact]
    public void GetCurrentSessionIdShouldReturnNullWhenNoSessionActive()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        string? currentSessionId = coordinator.GetCurrentSessionId();

        currentSessionId.ShouldBeNull();
    }

    [Fact]
    public void ResetTrackingDetailsShouldClearTransferHistory()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        SyncState? firstState = null;
        SyncState? secondState = null;
        var stateCount = 0;
        coordinator.Progress.Subscribe(state =>
        {
            stateCount++;
            if(stateCount == 1)
                firstState = state;
            // State 2 is emitted during ResetTrackingDetails, which doesn't publish a state
            else if(stateCount == 3)
                secondState = state;
        });

        coordinator.UpdateProgress("test-account", SyncStatus.Running, completedBytes: 10_485_760);
        Thread.Sleep(200);
        coordinator.ResetTrackingDetails(completedBytes: 10_485_760);
        Thread.Sleep(200);
        coordinator.UpdateProgress("test-account", SyncStatus.Running, completedBytes: 20_971_520);

        // Verify the first and third states were captured (reset doesn't publish)
        firstState.ShouldNotBeNull();
        secondState.ShouldNotBeNull();
        stateCount.ShouldBe(3); // Initial state + 2 UpdateProgress calls
    }

    [Fact]
    public void UpdateProgressShouldIncludeCurrentScanningFolder()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);

        SyncState? receivedState = null;
        coordinator.Progress.Subscribe(state => receivedState = state);

        coordinator.UpdateProgress("test-account", SyncStatus.Running, currentScanningFolder: "/Documents");

        receivedState.ShouldNotBeNull();
        receivedState.CurrentStatusMessage.ShouldBe("/Documents");
    }

    [Fact]
    public async Task RespectCancellationTokenWhenInitializingSession()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, cts.Token)
        );
    }

    [Fact]
    public async Task RespectCancellationTokenWhenRecordingCompletion()
    {
        var repository = new SyncSessionLogRepository(_contextFactory);
        var coordinator = new SyncStateCoordinator(repository);
        await coordinator.InitializeSessionAsync("test-account", enableDetailedLogging: true, CancellationToken.None);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await coordinator.RecordCompletionAsync(1, 1, 0, 0, 100, cts.Token)
        );
    }
}
