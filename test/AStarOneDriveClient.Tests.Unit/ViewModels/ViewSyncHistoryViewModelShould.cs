using AStarOneDriveClient.ViewModels;
using Shouldly;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class ViewSyncHistoryViewModelShould
{
    [Fact]
    public void InitializeSuccessfully()
    {
        var sut = new ViewSyncHistoryViewModel();

        sut.ShouldNotBeNull();
    }

    [Fact]
    public void ReturnPlaceholderMessage()
    {
        var message = ViewSyncHistoryViewModel.PlaceholderMessage;

        message.ShouldBe("Sync history viewing coming soon");
    }
}
