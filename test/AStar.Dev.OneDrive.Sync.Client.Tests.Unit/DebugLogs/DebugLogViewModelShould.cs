using System.Reactive.Linq;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.DebugLogs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.DebugLogs;

public class DebugLogViewModelShould
{
    private readonly IAccountRepository _mockAccountRepo;
    private readonly IDebugLogRepository _mockDebugLogRepo;
    private readonly IDebugLogger _mockDebugLogger;

    public DebugLogViewModelShould()
    {
        _mockAccountRepo = Substitute.For<IAccountRepository>();
        _mockDebugLogRepo = Substitute.For<IDebugLogRepository>();
        _mockDebugLogger = Substitute.For<IDebugLogger>();
    }

    [Fact]
    public void InitializeWithEmptyCollections()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.Accounts.ShouldBeEmpty();
        sut.DebugLogs.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithCurrentPageAsOne()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public void InitializeWithHasMoreRecordsAsTrue()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.HasMoreRecords.ShouldBeTrue();
    }

    [Fact]
    public void InitializeWithIsLoadingAsFalse()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithSelectedAccountAsNull()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeCommandsAsNotNull()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        _ = sut.LoadNextPageCommand.ShouldNotBeNull();
        _ = sut.LoadPreviousPageCommand.ShouldNotBeNull();
        _ = sut.ClearLogsCommand.ShouldNotBeNull();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountChanges()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(DebugLogViewModel.SelectedAccount))
                propertyChanged = true;
        };

        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
        sut.SelectedAccount.ShouldBe(account);
    }

    [Fact(Skip = "Requires refactor to allow it to run on CI/CD")]
    public async Task RaisePropertyChangedWhenCurrentPageChanges()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);
        var propertyChanged = false;

        // Set up account and load initial data - return 51 records to indicate HasMoreRecords
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockAccountRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([account]);

        var logs = new List<DebugLogEntry>();
        for(var i = 0; i < 51; i++)
            logs.Add(new DebugLogEntry(i, new HashedAccountId(AccountIdHasher.Hash("acc1")), DateTime.UtcNow, "Info", "Test", $"Message {i}", null));

        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(logs);

        sut.SelectedAccount = account;

        // Now subscribe to property changes
        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(DebugLogViewModel.CurrentPage))
                propertyChanged = true;
        };

        // Trigger page change by going to next page - need to await the observable
        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void ReturnCanGoToNextPageAsTrueWhenHasMoreRecords()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.CanGoToNextPage.ShouldBeTrue();
    }

    [Fact]
    public void ReturnCanGoToPreviousPageAsFalseWhenOnFirstPage()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.CanGoToPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void ReturnCanGoToNextPageAsFalseWhenIsLoading()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            IsLoading = true
        };

        sut.CanGoToNextPage.ShouldBeFalse();
    }

    [Fact]
    public void ReturnCanGoToPreviousPageAsFalseWhenIsLoading()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            IsLoading = true
        };

        sut.CanGoToPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithDefaultPageSizeOfFifty()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.PageSize.ShouldBe(50);
    }

    [Fact]
    public void InitializeWithTotalRecordCountOfZero()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        sut.TotalRecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task LoadAccountsIntoCollectionOnInitialization()
    {
        var account1 = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test1", @"C:\Path1", true, null, null, false, false, 3, 50, 0);
        var account2 = new AccountInfo("acc2", new HashedAccountId(AccountIdHasher.Hash("acc2")), "Test2", @"C:\Path2", true, null, null, false, false, 3, 50, 0);
        _ = _mockAccountRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([account1, account2]);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.ShouldContain(account1);
        sut.Accounts.ShouldContain(account2);
        sut.Accounts.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleExceptionWhenLoadingAccountsFails()
    {
        _ = _mockAccountRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<AccountInfo>>(new Exception("Test exception")));

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task LoadLogsWhenSelectedAccountIsSet()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        var logs = new List<DebugLogEntry>
        {
            new(1, new HashedAccountId(AccountIdHasher.Hash("acc1")), DateTime.UtcNow, "Info", "Source1", "Message1", null),
            new(2, new HashedAccountId(AccountIdHasher.Hash("acc1")), DateTime.UtcNow, "Error", "Source2", "Message2", "Stack trace")
        };

        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(logs);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(2);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.DebugLogs.Count.ShouldBe(2);
        sut.DebugLogs[0].Message.ShouldBe("Message1");
        sut.DebugLogs[1].Message.ShouldBe("Message2");
    }

    [Fact]
    public async Task UpdateTotalRecordCountWhenLoadingLogs()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        var logs = new List<DebugLogEntry>();

        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(logs);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(125);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.TotalRecordCount.ShouldBe(125);
    }

    [Fact]
    public async Task CalculateStartingCountOfItemsDisplayedCorrectly()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.StartingCountOfItemsDisplayed.ShouldBe(1);
    }

    [Fact]
    public async Task CalculateEndingCountOfItemsDisplayedCorrectly()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.EndingCountOfItemsDisplayed.ShouldBe(50);
    }

    [Fact]
    public async Task ResetCurrentPageToOneWhenSelectedAccountChanges()
    {
        var account1 = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test1", @"C:\Path1", true, null, null, false, false, 3, 50, 0);
        var account2 = new AccountInfo("acc2", new HashedAccountId(AccountIdHasher.Hash("acc2")), "Test2", @"C:\Path2", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account1
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();
        sut.CurrentPage.ShouldBe(2);

        sut.SelectedAccount = account2;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task IncrementPageNumberWhenLoadingNextPage()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.CurrentPage.ShouldBe(1);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(2);
    }

    [Fact]
    public async Task DecrementPageNumberWhenLoadingPreviousPage()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();
        sut.CurrentPage.ShouldBe(2);

        _ = await sut.LoadPreviousPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task NotLoadNextPageWhenHasMoreRecordsIsFalse()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        var initialPage = sut.CurrentPage;

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(initialPage);
    }

    [Fact]
    public async Task NotLoadNextPageWhenSelectedAccountIsNull()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);
        var initialPage = sut.CurrentPage;

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(initialPage);
    }

    [Fact]
    public async Task NotLoadPreviousPageWhenOnFirstPage()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        _ = await sut.LoadPreviousPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task NotLoadPreviousPageWhenSelectedAccountIsNull()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        _ = await sut.LoadPreviousPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task ClearLogsForSelectedAccount()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        var logs = new List<DebugLogEntry>
        {
            new(1, new HashedAccountId(AccountIdHasher.Hash("acc1")), DateTime.UtcNow, "Info", "Source1", "Message1", null)
        };

        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(logs);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(1);
        _ = _mockDebugLogRepo.DeleteByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.DebugLogs.Count.ShouldBe(1);

        _ = await sut.ClearLogsCommand.Execute().FirstAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        await _mockDebugLogRepo.Received(1).DeleteByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetToPageOneAfterClearingLogs()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);
        _ = _mockDebugLogRepo.DeleteByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();
        sut.CurrentPage.ShouldBe(2);

        _ = await sut.ClearLogsCommand.Execute().FirstAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task NotClearLogsWhenSelectedAccountIsNull()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);

        _ = await sut.ClearLogsCommand.Execute().FirstAsync();

        await _mockDebugLogRepo.DidNotReceive().DeleteByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetIsLoadingToTrueWhileClearingLogs()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        var clearingStarted = new TaskCompletionSource<bool>();
        var canComplete = new TaskCompletionSource<bool>();

        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(0);
        _ = _mockDebugLogRepo.DeleteByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            return DelayedCompletion();

            async Task DelayedCompletion()
            {
                clearingStarted.SetResult(true);
                await canComplete.Task;
            }
        });

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        IObservable<System.Reactive.Unit> clearTask = sut.ClearLogsCommand.Execute();

        _ = await clearingStarted.Task;
        sut.IsLoading.ShouldBeTrue();

        canComplete.SetResult(true);
        _ = await clearTask.FirstAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleExceptionDuringClearLogs()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(0);
        _ = _mockDebugLogRepo.DeleteByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new Exception("Delete failed")));

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.ClearLogsCommand.Execute().FirstAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task CallRepositoryWithCorrectPaginationParametersOnFirstPage()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        _ = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await _mockDebugLogRepo.Received(1).GetByAccountIdAsync(account.HashedAccountId, 50, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CallRepositoryWithCorrectPaginationParametersOnSecondPage()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        _ = await _mockDebugLogRepo.Received(1).GetByAccountIdAsync(account.HashedAccountId, 50, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearDebugLogsCollectionBeforeLoadingNewPage()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        var firstPageLogs = new List<DebugLogEntry>
        {
            new(1, new HashedAccountId(AccountIdHasher.Hash("acc1")), DateTime.UtcNow, "Info", "Source1", "Page 1", null)
        };
        var secondPageLogs = new List<DebugLogEntry>
        {
            new(2, new HashedAccountId(AccountIdHasher.Hash("acc1")), DateTime.UtcNow, "Info", "Source2", "Page 2", null)
        };

        _ = _mockDebugLogRepo.GetByAccountIdAsync(account.HashedAccountId, 50, 0, Arg.Any<CancellationToken>()).Returns(firstPageLogs);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(account.HashedAccountId, 50, 50, Arg.Any<CancellationToken>()).Returns(secondPageLogs);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.DebugLogs.Count.ShouldBe(1);
        sut.DebugLogs[0].Message.ShouldBe("Page 1");

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.DebugLogs.Count.ShouldBe(1);
        sut.DebugLogs[0].Message.ShouldBe("Page 2");
    }

    [Fact]
    public async Task ExecuteLoadNextPageCommandSuccessfully()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteLoadPreviousPageCommandSuccessfully()
    {
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _ = _mockDebugLogRepo.GetDebugLogCountByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(100);

        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger)
        {
            SelectedAccount = account
        };
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();
        sut.CurrentPage.ShouldBe(2);

        _ = await sut.LoadPreviousPageCommand.Execute().FirstAsync();

        sut.CurrentPage.ShouldBe(1);
    }
}
