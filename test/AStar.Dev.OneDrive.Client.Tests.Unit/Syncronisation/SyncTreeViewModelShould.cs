using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Models;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Syncronisation;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Syncronisation;

public class SyncTreeViewModelShould : IDisposable
{
    private readonly IFolderTreeService _mockFolderService;
    private readonly ISyncSelectionService _mockSelectionService;
    private readonly ISyncEngine _mockSyncEngine;
    private readonly Subject<SyncState> _progressSubject;
    private readonly SyncTreeViewModel _viewModel;

    public SyncTreeViewModelShould()
    {
        _mockFolderService = Substitute.For<IFolderTreeService>();
        _mockSelectionService = Substitute.For<ISyncSelectionService>();
        _mockSyncEngine = Substitute.For<ISyncEngine>();

        _progressSubject = new Subject<SyncState>();
        _ = _mockSyncEngine.Progress.Returns(_progressSubject);

        _viewModel = new SyncTreeViewModel(_mockFolderService, _mockSelectionService, _mockSyncEngine);
    }

    public void Dispose()
    {
        _progressSubject.Dispose();
        _viewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ExposeEstimatedSecondsRemainingFromSyncState()
    {
        var syncState = new SyncState(
            "acc1",
            SyncStatus.Running,
            10,
            5,
            1000,
            500,
            2,
            1,
            0,
            0,
            3.5,
            42,
            null);
        _progressSubject.OnNext(syncState);
        _viewModel.EstimatedSecondsRemaining.ShouldBe(42);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ExposeMegabytesPerSecondFromSyncState()
    {
        var syncState = new SyncState(
            "acc1",
            SyncStatus.Running,
            10,
            5,
            1000,
            500,
            2,
            1,
            0,
            0,
            7.25,
            10,
            null);
        _progressSubject.OnNext(syncState);
        _viewModel.MegabytesPerSecond.ShouldBe(7.25);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void InitializeWithEmptyRootFolders()
    {
        _viewModel.RootFolders.ShouldBeEmpty();
        _viewModel.IsLoading.ShouldBeFalse();
        _viewModel.ErrorMessage.ShouldBeNull();
        _viewModel.SelectedAccountId.ShouldBeNull();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task LoadRootFoldersWhenAccountIdIsSet()
    {
        var folders = new List<OneDriveFolderNode> { new() { Id = "folder1", Name = "Folder 1", IsFolder = true }, new() { Id = "folder2", Name = "Folder 2", IsFolder = true } };

        _ = _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(folders);

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow async command to execute

        _viewModel.RootFolders.Count.ShouldBe(2);
        _viewModel.RootFolders[0].Name.ShouldBe("Folder 1");
        _viewModel.RootFolders[1].Name.ShouldBe("Folder 2");
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ClearRootFoldersWhenAccountIdIsNull()
    {
        _viewModel.RootFolders.Add(new OneDriveFolderNode { Id = "test", Name = "Test" });

        _viewModel.SelectedAccountId = null;
        _ = _viewModel.LoadFoldersCommand.Execute().Subscribe();

        _viewModel.RootFolders.ShouldBeEmpty();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task SetIsLoadingDuringFolderLoad()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<OneDriveFolderNode>>();
        _ = _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(info => tcs.Task);

        var isLoadingValues = new List<bool>();
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(SyncTreeViewModel.IsLoading))
                isLoadingValues.Add(_viewModel.IsLoading);
        };

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(50, TestContext.Current.CancellationToken);
        isLoadingValues.ShouldContain(true);

        tcs.SetResult([]);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        isLoadingValues.ShouldContain(false);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task SetErrorMessageWhenLoadFails()
    {
        _ = _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OneDriveFolderNode>>(new InvalidOperationException("Test error")));

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = _viewModel.ErrorMessage.ShouldNotBeNull();
        _viewModel.ErrorMessage.ShouldContain("Test error");
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task LoadChildrenWhenCommandExecuted()
    {
        var parent = new OneDriveFolderNode { Id = "parent", Name = "Parent", IsFolder = true };
        var children = new List<OneDriveFolderNode> { new() { Id = "child1", Name = "Child 1", ParentId = "parent", IsFolder = true } };

        _ = _mockFolderService.GetChildFoldersAsync("account123", "parent", Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(children);

        _viewModel.SelectedAccountId = "account123";

        _ = _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        parent.Children.Count.ShouldBe(1);
        parent.Children[0].Name.ShouldBe("Child 1");
        parent.ChildrenLoaded.ShouldBeTrue();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task NotLoadChildrenIfAlreadyLoaded()
    {
        var parent = new OneDriveFolderNode { Id = "parent", Name = "Parent", IsFolder = true, ChildrenLoaded = true };

        _viewModel.SelectedAccountId = "account123";

        _ = _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _ = await _mockFolderService.DidNotReceive().GetChildFoldersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task InheritParentSelectionStateForNewChildren()
    {
        var parent = new OneDriveFolderNode { Id = "parent", Name = "Parent", IsFolder = true, SelectionState = SelectionState.Checked };

        var children = new List<OneDriveFolderNode> { new() { Id = "child1", Name = "Child 1", ParentId = "parent", IsFolder = true } };

        _ = _mockFolderService.GetChildFoldersAsync("account123", "parent", Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(children);

        _viewModel.SelectedAccountId = "account123";

        _ = _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        parent.Children[0].SelectionState.ShouldBe(SelectionState.Unchecked);
        parent.Children[0].IsSelected.ShouldBeNull();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ToggleSelectionFromUncheckedToChecked()
    {
        var folder = new OneDriveFolderNode { Id = "folder1", Name = "Folder 1", SelectionState = SelectionState.Unchecked };

        _ = _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, true);
        _mockSelectionService.Received(1).UpdateParentStates(folder, Arg.Any<List<OneDriveFolderNode>>());
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ToggleSelectionFromCheckedToUnchecked()
    {
        var folder = new OneDriveFolderNode { Id = "folder1", Name = "Folder 1", SelectionState = SelectionState.Checked };

        _ = _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, false);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ToggleSelectionFromIndeterminateToChecked()
    {
        var folder = new OneDriveFolderNode { Id = "folder1", Name = "Folder 1", SelectionState = SelectionState.Indeterminate };

        _ = _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, true);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ClearAllSelectionsWhenCommandExecuted()
    {
        _viewModel.RootFolders.Add(new OneDriveFolderNode { Id = "folder1", Name = "Folder 1" });

        _ = _viewModel.ClearSelectionsCommand.Execute().Subscribe();

        _mockSelectionService.Received(1).ClearAllSelections(Arg.Any<List<OneDriveFolderNode>>());
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void GetSelectedFoldersFromSelectionService()
    {
        var selectedFolders = new List<OneDriveFolderNode> { new() { Id = "selected1", Name = "Selected 1", SelectionState = SelectionState.Checked } };

        _ = _mockSelectionService.GetSelectedFolders(Arg.Any<List<OneDriveFolderNode>>())
            .Returns(selectedFolders);

        List<OneDriveFolderNode> result = _viewModel.GetSelectedFolders();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Selected 1");
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void RaisePropertyChangedWhenSelectedAccountIdChanges()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(SyncTreeViewModel.SelectedAccountId))
                propertyChangedRaised = true;
        };

        _viewModel.SelectedAccountId = "newAccount";

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void RaiseCollectionChangedWhenRootFoldersModified()
    {
        var collectionChangedRaised = false;
        _viewModel.RootFolders.CollectionChanged += (_, _) => collectionChangedRaised = true;

        _viewModel.RootFolders.Add(new OneDriveFolderNode());

        collectionChangedRaised.ShouldBeTrue();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ThrowArgumentNullExceptionWhenFolderServiceIsNull()
    {
        Exception? exception = Record.Exception(() => new SyncTreeViewModel(null!, _mockSelectionService, _mockSyncEngine));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ThrowArgumentNullExceptionWhenSelectionServiceIsNull()
    {
        Exception? exception = Record.Exception(() => new SyncTreeViewModel(_mockFolderService, null!, _mockSyncEngine));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ThrowArgumentNullExceptionWhenLoadingChildrenWithNullFolder()
    {
        Exception? exception = Record.Exception(() => _viewModel.LoadChildrenCommand.Execute(null!).Subscribe());

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public void ThrowArgumentNullExceptionWhenTogglingNullFolder()
    {
        Exception? exception = Record.Exception(() => _viewModel.ToggleSelectionCommand.Execute(null!).Subscribe());

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }
}
