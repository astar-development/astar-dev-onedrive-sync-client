using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenASyncSchedulerStaticDefaults
{
    [Fact]
    public void when_default_interval_is_read_then_it_equals_sixty_minutes() =>
        SyncScheduler.DefaultInterval.ShouldBe(TimeSpan.FromMinutes(60));
}

public sealed class GivenASyncSchedulerWithNoAccounts
{
    private readonly ISyncService syncService = Substitute.For<ISyncService>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();

    public GivenASyncSchedulerWithNoAccounts() =>
        accountRepository.GetAllAsync().Returns(new List<AccountEntity>());

    [Fact]
    public async Task when_trigger_now_is_called_then_get_all_async_is_called_exactly_once()
    {
        var sut = new SyncScheduler(syncService, accountRepository);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        _ = await accountRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_account_async_is_never_called()
    {
        var sut = new SyncScheduler(syncService, accountRepository);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        await syncService.DidNotReceive().SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }
}

public sealed class GivenASyncSchedulerWithOneAccount
{
    private const string AccountId = "acc-001";
    private const string AccountEmail = "user@outlook.com";
    private const string AccountDisplayName = "Test User";

    private readonly ISyncService syncService = Substitute.For<ISyncService>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();

    public GivenASyncSchedulerWithOneAccount()
    {
        var entity = new AccountEntity
        {
            Id          = AccountId,
            Email       = AccountEmail,
            DisplayName = AccountDisplayName,
            SyncFolders = []
        };
        accountRepository.GetAllAsync().Returns(new List<AccountEntity> { entity });
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_account_async_is_called_exactly_once()
    {
        var sut = new SyncScheduler(syncService, accountRepository);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        await syncService.Received(1).SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_started_event_is_raised_with_account_id()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        string? capturedId = null;
        sut.SyncStarted += (_, id) => capturedId = id;

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        capturedId.ShouldBe(AccountId);
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_completed_event_is_raised_with_account_id()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        string? capturedId = null;
        sut.SyncCompleted += (_, id) => capturedId = id;

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        capturedId.ShouldBe(AccountId);
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_account_passed_to_sync_has_correct_id()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        OneDriveAccount? captured = null;
        await syncService.SyncAccountAsync(Arg.Do<OneDriveAccount>(a => captured = a), Arg.Any<CancellationToken>());

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        captured?.Id.ShouldBe(AccountId);
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_account_passed_to_sync_has_correct_email()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        OneDriveAccount? captured = null;
        await syncService.SyncAccountAsync(Arg.Do<OneDriveAccount>(a => captured = a), Arg.Any<CancellationToken>());

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        captured?.Email.ShouldBe(AccountEmail);
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_account_passed_to_sync_has_correct_display_name()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        OneDriveAccount? captured = null;
        await syncService.SyncAccountAsync(Arg.Do<OneDriveAccount>(a => captured = a), Arg.Any<CancellationToken>());

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        captured?.DisplayName.ShouldBe(AccountDisplayName);
    }
}

public sealed class GivenASyncSchedulerWithMultipleAccounts
{
    private readonly ISyncService syncService = Substitute.For<ISyncService>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();

    public GivenASyncSchedulerWithMultipleAccounts()
    {
        var entities = new List<AccountEntity>
        {
            new() { Id = "acc-a", Email = "a@outlook.com", DisplayName = "Alpha", SyncFolders = [] },
            new() { Id = "acc-b", Email = "b@outlook.com", DisplayName = "Beta",  SyncFolders = [] },
            new() { Id = "acc-c", Email = "c@outlook.com", DisplayName = "Gamma", SyncFolders = [] }
        };
        accountRepository.GetAllAsync().Returns(entities);
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_account_async_is_called_once_per_account()
    {
        var sut = new SyncScheduler(syncService, accountRepository);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        await syncService.Received(3).SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_started_is_raised_once_per_account()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        var raisedCount = 0;
        sut.SyncStarted += (_, _) => raisedCount++;

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        raisedCount.ShouldBe(3);
    }

    [Fact]
    public async Task when_trigger_now_is_called_then_sync_completed_is_raised_once_per_account()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        var raisedCount = 0;
        sut.SyncCompleted += (_, _) => raisedCount++;

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        raisedCount.ShouldBe(3);
    }
}

public sealed class GivenASyncSchedulerWhenSyncServiceThrowsForOneAccount
{
    private const string FirstAccountId  = "acc-first";
    private const string SecondAccountId = "acc-second";

    private readonly ISyncService syncService = Substitute.For<ISyncService>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();

    public GivenASyncSchedulerWhenSyncServiceThrowsForOneAccount()
    {
        var entities = new List<AccountEntity>
        {
            new() { Id = FirstAccountId,  Email = "first@outlook.com",  DisplayName = "First",  SyncFolders = [] },
            new() { Id = SecondAccountId, Email = "second@outlook.com", DisplayName = "Second", SyncFolders = [] }
        };
        accountRepository.GetAllAsync().Returns(entities);

        syncService
            .SyncAccountAsync(Arg.Is<OneDriveAccount>(a => a.Id == FirstAccountId), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Sync failed for first account")));
    }

    [Fact]
    public async Task when_first_account_throws_then_sync_completed_is_still_raised_for_first_account()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        var completedIds = new List<string>();
        sut.SyncCompleted += (_, id) => completedIds.Add(id);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        completedIds.ShouldContain(FirstAccountId);
    }

    [Fact]
    public async Task when_first_account_throws_then_second_account_is_still_synced()
    {
        var sut = new SyncScheduler(syncService, accountRepository);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        await syncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == SecondAccountId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_first_account_throws_then_sync_started_is_raised_for_both_accounts()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        var startedIds = new List<string>();
        sut.SyncStarted += (_, id) => startedIds.Add(id);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        startedIds.ShouldContain(FirstAccountId);
        startedIds.ShouldContain(SecondAccountId);
    }

    [Fact]
    public async Task when_first_account_throws_then_sync_completed_is_raised_for_both_accounts()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        var completedIds = new List<string>();
        sut.SyncCompleted += (_, id) => completedIds.Add(id);

        await sut.TriggerNowAsync(TestContext.Current.CancellationToken);

        completedIds.ShouldContain(FirstAccountId);
        completedIds.ShouldContain(SecondAccountId);
    }
}

public sealed class GivenATriggerAccountAsync
{
    private const string AccountId = "direct-acc-001";

    private readonly ISyncService syncService = Substitute.For<ISyncService>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
    private readonly OneDriveAccount account = new() { Id = AccountId, Email = "direct@outlook.com", DisplayName = "Direct User" };

    [Fact]
    public async Task when_trigger_account_async_is_called_then_sync_account_async_is_called_with_the_exact_account_object()
    {
        var sut = new SyncScheduler(syncService, accountRepository);

        await sut.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        await syncService.Received(1).SyncAccountAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_trigger_account_async_is_called_then_sync_started_event_is_raised_with_account_id()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        string? capturedId = null;
        sut.SyncStarted += (_, id) => capturedId = id;

        await sut.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        capturedId.ShouldBe(AccountId);
    }

    [Fact]
    public async Task when_trigger_account_async_is_called_then_sync_completed_event_is_raised_with_account_id()
    {
        var sut = new SyncScheduler(syncService, accountRepository);
        string? capturedId = null;
        sut.SyncCompleted += (_, id) => capturedId = id;

        await sut.TriggerAccountAsync(account, TestContext.Current.CancellationToken);

        capturedId.ShouldBe(AccountId);
    }

    [Fact]
    public async Task when_sync_account_async_throws_then_sync_completed_is_still_raised()
    {
        syncService
            .SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Sync failed")));

        var sut = new SyncScheduler(syncService, accountRepository);
        var completedRaised = false;
        sut.SyncCompleted += (_, _) => completedRaised = true;

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.TriggerAccountAsync(account, TestContext.Current.CancellationToken));

        completedRaised.ShouldBeTrue();
    }
}
