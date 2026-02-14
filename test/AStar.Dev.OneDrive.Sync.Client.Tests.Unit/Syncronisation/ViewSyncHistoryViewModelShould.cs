using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Syncronisation;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Syncronisation;

public class ViewSyncHistoryViewModelShould
{
    private readonly IDebugLogger _debugLogger;
    private readonly IAccountRepository _mockAccountRepo;
    private readonly IFileOperationLogRepository _mockFileOpLogRepo;

    public ViewSyncHistoryViewModelShould()
    {
        _debugLogger = Substitute.For<IDebugLogger>();

        _mockAccountRepo = Substitute.For<IAccountRepository>();
        _mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();
    }

    [Fact]
    public void InitializeWithEmptySyncHistoryCollection()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.SyncHistory.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithNullSelectedAccount()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithCurrentPageOne()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public void InitializeWithHasMoreRecordsTrue()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.HasMoreRecords.ShouldBeTrue();
    }

    [Fact]
    public void InitializeWithIsLoadingFalse()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void InitializeLoadNextPageCommand()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        _ = sut.LoadNextPageCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeLoadPreviousPageCommand()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        _ = sut.LoadPreviousPageCommand.ShouldNotBeNull();
    }

    [Fact]
    public void ReturnCanGoToPreviousPageFalseWhenOnFirstPage()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.CanGoToPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void ReturnCanGoToNextPageTrueWhenHasMoreRecords()
    {
        var sut = new ViewSyncHistoryViewModel(_mockAccountRepo, _mockFileOpLogRepo, _debugLogger);

        sut.CanGoToNextPage.ShouldBeTrue();
    }
}
