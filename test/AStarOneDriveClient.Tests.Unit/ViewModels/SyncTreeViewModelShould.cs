using System.Reactive.Subjects;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.ViewModels;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

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
        _mockSyncEngine.Progress.Returns(_progressSubject);

        _viewModel = new SyncTreeViewModel(_mockFolderService, _mockSelectionService, _mockSyncEngine);
    }

    [Fact]
    public void InitializeWithEmptyRootFolders()
    {
        _viewModel.RootFolders.ShouldBeEmpty();
        _viewModel.IsLoading.ShouldBeFalse();
        _viewModel.ErrorMessage.ShouldBeNull();
        _viewModel.SelectedAccountId.ShouldBeNull();
    }

    [Fact]
    public async Task LoadRootFoldersWhenAccountIdIsSet()
    {
        var folders = new List<OneDriveFolderNode>
        {
            new() { Id = "folder1", Name = "Folder 1", IsFolder = true },
            new() { Id = "folder2", Name = "Folder 2", IsFolder = true }
        };

        _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(folders);

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(100); // Allow async command to execute

        _viewModel.RootFolders.Count.ShouldBe(2);
        _viewModel.RootFolders[0].Name.ShouldBe("Folder 1");
        _viewModel.RootFolders[1].Name.ShouldBe("Folder 2");
    }

    [Fact]
    public void ClearRootFoldersWhenAccountIdIsNull()
    {
        _viewModel.RootFolders.Add(new OneDriveFolderNode { Id = "test", Name = "Test" });

        _viewModel.SelectedAccountId = null;
        _viewModel.LoadFoldersCommand.Execute().Subscribe();

        _viewModel.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetIsLoadingDuringFolderLoad()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<OneDriveFolderNode>>();
        _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(info => tcs.Task);

        var isLoadingValues = new List<bool>();
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SyncTreeViewModel.IsLoading))
            {
                isLoadingValues.Add(_viewModel.IsLoading);
            }
        };

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(50);
        isLoadingValues.ShouldContain(true);

        tcs.SetResult([]);
        await Task.Delay(50);

        isLoadingValues.ShouldContain(false);
    }

    [Fact]
    public async Task SetErrorMessageWhenLoadFails()
    {
        _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OneDriveFolderNode>>(new InvalidOperationException("Test error")));

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(100);

        _viewModel.ErrorMessage.ShouldNotBeNull();
        _viewModel.ErrorMessage.ShouldContain("Test error");
    }

    [Fact]
    public async Task LoadChildrenWhenCommandExecuted()
    {
        var parent = new OneDriveFolderNode { Id = "parent", Name = "Parent", IsFolder = true };
        var children = new List<OneDriveFolderNode>
        {
            new() { Id = "child1", Name = "Child 1", ParentId = "parent", IsFolder = true }
        };

        _mockFolderService.GetChildFoldersAsync("account123", "parent", Arg.Any<CancellationToken>())
            .Returns(children);

        _viewModel.SelectedAccountId = "account123";

        _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100);

        parent.Children.Count.ShouldBe(1);
        parent.Children[0].Name.ShouldBe("Child 1");
        parent.ChildrenLoaded.ShouldBeTrue();
    }

    [Fact]
    public async Task NotLoadChildrenIfAlreadyLoaded()
    {
        var parent = new OneDriveFolderNode
        {
            Id = "parent",
            Name = "Parent",
            IsFolder = true,
            ChildrenLoaded = true
        };

        _viewModel.SelectedAccountId = "account123";

        _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100);

        await _mockFolderService.DidNotReceive().GetChildFoldersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InheritParentSelectionStateForNewChildren()
    {
        var parent = new OneDriveFolderNode
        {
            Id = "parent",
            Name = "Parent",
            IsFolder = true,
            SelectionState = SelectionState.Checked
        };

        var children = new List<OneDriveFolderNode>
        {
            new() { Id = "child1", Name = "Child 1", ParentId = "parent", IsFolder = true }
        };

        _mockFolderService.GetChildFoldersAsync("account123", "parent", Arg.Any<CancellationToken>())
            .Returns(children);

        _viewModel.SelectedAccountId = "account123";

        _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100);

        parent.Children[0].SelectionState.ShouldBe(SelectionState.Unchecked);
        parent.Children[0].IsSelected.ShouldBeNull();
    }

    [Fact]
    public void ToggleSelectionFromUncheckedToChecked()
    {
        var folder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Folder 1",
            SelectionState = SelectionState.Unchecked
        };

        _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, true);
        _mockSelectionService.Received(1).UpdateParentStates(folder, Arg.Any<List<OneDriveFolderNode>>());
    }

    [Fact]
    public void ToggleSelectionFromCheckedToUnchecked()
    {
        var folder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Folder 1",
            SelectionState = SelectionState.Checked
        };

        _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, false);
    }

    [Fact]
    public void ToggleSelectionFromIndeterminateToChecked()
    {
        var folder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Folder 1",
            SelectionState = SelectionState.Indeterminate
        };

        _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, true);
    }

    [Fact]
    public void ClearAllSelectionsWhenCommandExecuted()
    {
        _viewModel.RootFolders.Add(new OneDriveFolderNode { Id = "folder1", Name = "Folder 1" });

        _viewModel.ClearSelectionsCommand.Execute().Subscribe();

        _mockSelectionService.Received(1).ClearAllSelections(Arg.Any<List<OneDriveFolderNode>>());
    }

    [Fact]
    public void GetSelectedFoldersFromSelectionService()
    {
        var selectedFolders = new List<OneDriveFolderNode>
        {
            new() { Id = "selected1", Name = "Selected 1", SelectionState = SelectionState.Checked }
        };

        _mockSelectionService.GetSelectedFolders(Arg.Any<List<OneDriveFolderNode>>())
            .Returns(selectedFolders);

        var result = _viewModel.GetSelectedFolders();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Selected 1");
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountIdChanges()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SyncTreeViewModel.SelectedAccountId))
            {
                propertyChangedRaised = true;
            }
        };

        _viewModel.SelectedAccountId = "newAccount";

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void RaiseCollectionChangedWhenRootFoldersModified()
    {
        var collectionChangedRaised = false;
        _viewModel.RootFolders.CollectionChanged += (_, _) => collectionChangedRaised = true;

        _viewModel.RootFolders.Add(new OneDriveFolderNode());

        collectionChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenFolderServiceIsNull()
    {
        var exception = Record.Exception(() => new SyncTreeViewModel(null!, _mockSelectionService, _mockSyncEngine));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSelectionServiceIsNull()
    {
        var exception = Record.Exception(() => new SyncTreeViewModel(_mockFolderService, null!, _mockSyncEngine));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenLoadingChildrenWithNullFolder()
    {
        var exception = Record.Exception(() => _viewModel.LoadChildrenCommand.Execute(null!).Subscribe());

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenTogglingNullFolder()
    {
        var exception = Record.Exception(() => _viewModel.ToggleSelectionCommand.Execute(null!).Subscribe());

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    public void Dispose()
    {
        _progressSubject.Dispose();
        _viewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
