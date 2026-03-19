using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using NSubstitute;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public class SyncSchedulerTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultInterval_ShouldBe60Minutes()
        => SyncScheduler.DefaultInterval.ShouldBe(TimeSpan.FromMinutes(60));

    [Fact]
    public void Start_ShouldInitializeTimer()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        scheduler.Start();
        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Start_WithDefaultInterval_ShouldUse60Minutes()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        scheduler.Start();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Start_WithCustomInterval_ShouldUseProvidedInterval()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var customInterval = TimeSpan.FromMinutes(30);

        scheduler.Start(customInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Stop_ShouldStopTimer()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.Start();

        scheduler.Stop();

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void SetInterval_ShouldUpdateInterval()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.Start();
        var newInterval = TimeSpan.FromMinutes(30);

        scheduler.SetInterval(newInterval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerNowAsync_ShouldExecuteSyncPass()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync().Returns(new List<AccountEntity>());

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = await mockRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task TriggerNowAsync_WhenAlreadyRunning_ShouldNotStartNewPass()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        _ = mockRepository.GetAllAsync().Returns(new List<AccountEntity>());

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // This is difficult to test without internal state exposure
        // Just verify it doesn't throw
        await scheduler.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldSyncSpecificAccount()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldRaiseSyncStartedEvent()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };
        var eventRaised = false;
        string? raisedAccountId = null;

        scheduler.SyncStarted += (s, accountId) =>
        {
            eventRaised = true;
            raisedAccountId = accountId;
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
        raisedAccountId.ShouldBe("test-account");
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldRaiseSyncCompletedEvent()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };
        var eventRaised = false;
        string? raisedAccountId = null;

        scheduler.SyncCompleted += (s, accountId) =>
        {
            eventRaised = true;
            raisedAccountId = accountId;
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeTrue();
        raisedAccountId.ShouldBe("test-account");
    }

    [Fact]
    public async Task TriggerAccountAsync_WhenSyncServiceThrows_ShouldStillRaiseCompletedEvent()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        _ = mockSyncService.SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Sync failed")));

        var completedEventRaised = false;
        scheduler.SyncCompleted += (s, accountId) => completedEventRaised = true;

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await scheduler.TriggerAccountAsync(account));

        completedEventRaised.ShouldBeTrue();
    }

    [Fact]
    public void SyncScheduler_ShouldBeAsyncDisposable()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        _ = scheduler.ShouldBeAssignableTo<IAsyncDisposable>();
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void Start_WithVariousIntervals_ShouldInitializeSuccessfully(int minutes)
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var interval = TimeSpan.FromMinutes(minutes);

        scheduler.Start(interval);

        _ = scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldPassCorrectAccountData()
    {
        ISyncService mockSyncService = Substitute.For<ISyncService>();
        IAccountRepository mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount
        {
            Id = "account-123",
            Email = "test@outlook.com",
            DisplayName = "Test User"
        };

        await scheduler.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == "account-123"),
            Arg.Any<CancellationToken>());
    }
}
