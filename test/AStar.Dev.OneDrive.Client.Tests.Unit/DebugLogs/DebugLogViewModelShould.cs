using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.DebugLogs;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.DebugLogs;

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

        var account = new AccountInfo("acc1", "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
        sut.SelectedAccount.ShouldBe(account);
    }

    [Fact]
    public async Task RaisePropertyChangedWhenCurrentPageChanges()
    {
        var sut = new DebugLogViewModel(_mockAccountRepo, _mockDebugLogRepo, _mockDebugLogger);
        var propertyChanged = false;

        // Set up account and load initial data - return 51 records to indicate HasMoreRecords
        var account = new AccountInfo("acc1", "Test", @"C:\Path", true, null, null, false, false, 3, 50, 0);
        _ = _mockAccountRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([account]);

        var logs = new List<DebugLogEntry>();
        for(var i = 0; i < 51; i++)
            logs.Add(new DebugLogEntry(i, "acc1", DateTime.UtcNow, "Info", "Test", $"Message {i}", null));

        _ = _mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
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
}
