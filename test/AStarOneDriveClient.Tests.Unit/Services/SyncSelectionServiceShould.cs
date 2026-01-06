using AStarOneDriveClient.Models;
using AStarOneDriveClient.Services;

namespace AStarOneDriveClient.Tests.Unit.Services;

public class SyncSelectionServiceShould
{
    private readonly SyncSelectionService _service = new();

    [Fact]
    public void SelectFolderAndSetPropertiesCorrectly()
    {
        var folder = CreateFolder("folder1", "Folder 1");

        _service.SetSelection(folder, true);

        folder.SelectionState.ShouldBe(SelectionState.Checked);
        folder.IsSelected.ShouldBe(true);
    }

    [Fact]
    public void DeselectFolderAndSetPropertiesCorrectly()
    {
        var folder = CreateFolder("folder1", "Folder 1");
        folder.SelectionState = SelectionState.Checked;
        folder.IsSelected = true;

        _service.SetSelection(folder, false);

        folder.SelectionState.ShouldBe(SelectionState.Unchecked);
        folder.IsSelected.ShouldBe(false);
    }

    [Fact]
    public void CascadeSelectionToAllChildren()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");
        var grandchild = CreateFolder("grandchild", "Grandchild", "child1");

        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.Children.Add(grandchild);

        _service.SetSelection(parent, true);

        parent.SelectionState.ShouldBe(SelectionState.Checked);
        child1.SelectionState.ShouldBe(SelectionState.Checked);
        child2.SelectionState.ShouldBe(SelectionState.Checked);
        grandchild.SelectionState.ShouldBe(SelectionState.Checked);
        grandchild.IsSelected.ShouldBe(true);
    }

    [Fact]
    public void CascadeDeselectionToAllChildren()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Checked;
        child2.SelectionState = SelectionState.Checked;

        _service.SetSelection(parent, false);

        parent.SelectionState.ShouldBe(SelectionState.Unchecked);
        child1.SelectionState.ShouldBe(SelectionState.Unchecked);
        child2.SelectionState.ShouldBe(SelectionState.Unchecked);
        child1.IsSelected.ShouldBe(false);
        child2.IsSelected.ShouldBe(false);
    }

    [Fact]
    public void CalculateCheckedStateWhenAllChildrenAreChecked()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Checked;
        child2.SelectionState = SelectionState.Checked;

        var state = _service.CalculateStateFromChildren(parent);

        state.ShouldBe(SelectionState.Checked);
    }

    [Fact]
    public void CalculateUncheckedStateWhenAllChildrenAreUnchecked()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Unchecked;
        child2.SelectionState = SelectionState.Unchecked;

        var state = _service.CalculateStateFromChildren(parent);

        state.ShouldBe(SelectionState.Unchecked);
    }

    [Fact]
    public void CalculateIndeterminateStateWhenChildrenHaveMixedStates()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Checked;
        child2.SelectionState = SelectionState.Unchecked;

        var state = _service.CalculateStateFromChildren(parent);

        state.ShouldBe(SelectionState.Indeterminate);
    }

    [Fact]
    public void CalculateIndeterminateStateWhenAnyChildIsIndeterminate()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Indeterminate;
        child2.SelectionState = SelectionState.Checked;

        var state = _service.CalculateStateFromChildren(parent);

        state.ShouldBe(SelectionState.Indeterminate);
    }

    [Fact]
    public void ReturnFolderOwnStateWhenNoChildren()
    {
        var folder = CreateFolder("folder", "Folder");
        folder.SelectionState = SelectionState.Checked;

        var state = _service.CalculateStateFromChildren(folder);

        state.ShouldBe(SelectionState.Checked);
    }

    [Fact]
    public void UpdateParentStateBasedOnChildren()
    {
        var parent = CreateFolder("parent", "Parent");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Checked;
        child2.SelectionState = SelectionState.Unchecked;

        var rootFolders = new List<OneDriveFolderNode> { parent };

        _service.UpdateParentStates(child1, rootFolders);

        parent.SelectionState.ShouldBe(SelectionState.Indeterminate);
        parent.IsSelected.ShouldBeNull();
    }

    [Fact]
    public void PropagateParentStateUpToRoot()
    {
        var root = CreateFolder("root", "Root");
        var parent = CreateFolder("parent", "Parent", "root");
        var child1 = CreateFolder("child1", "Child 1", "parent");
        var child2 = CreateFolder("child2", "Child 2", "parent");

        root.Children.Add(parent);
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        child1.SelectionState = SelectionState.Checked;
        child2.SelectionState = SelectionState.Unchecked;

        var rootFolders = new List<OneDriveFolderNode> { root };

        _service.UpdateParentStates(child1, rootFolders);

        parent.SelectionState.ShouldBe(SelectionState.Indeterminate);
        root.SelectionState.ShouldBe(SelectionState.Indeterminate);
    }

    [Fact]
    public void NotUpdateParentWhenFolderHasNoParent()
    {
        var folder = CreateFolder("folder", "Folder");
        folder.SelectionState = SelectionState.Checked;

        var rootFolders = new List<OneDriveFolderNode> { folder };

        _service.UpdateParentStates(folder, rootFolders);

        folder.SelectionState.ShouldBe(SelectionState.Checked);
    }

    [Fact]
    public void GetAllSelectedFoldersRecursively()
    {
        var root = CreateFolder("root", "Root");
        var child1 = CreateFolder("child1", "Child 1", "root");
        var child2 = CreateFolder("child2", "Child 2", "root");
        var grandchild = CreateFolder("grandchild", "Grandchild", "child1");

        root.Children.Add(child1);
        root.Children.Add(child2);
        child1.Children.Add(grandchild);

        root.SelectionState = SelectionState.Indeterminate;
        child1.SelectionState = SelectionState.Checked;
        child2.SelectionState = SelectionState.Unchecked;
        grandchild.SelectionState = SelectionState.Checked;

        var rootFolders = new List<OneDriveFolderNode> { root };

        var selectedFolders = _service.GetSelectedFolders(rootFolders);

        selectedFolders.Count.ShouldBe(2);
        selectedFolders.ShouldContain(child1);
        selectedFolders.ShouldContain(grandchild);
        selectedFolders.ShouldNotContain(root);
        selectedFolders.ShouldNotContain(child2);
    }

    [Fact]
    public void ReturnEmptyListWhenNoFoldersSelected()
    {
        var root = CreateFolder("root", "Root");
        var child = CreateFolder("child", "Child", "root");

        root.Children.Add(child);

        root.SelectionState = SelectionState.Unchecked;
        child.SelectionState = SelectionState.Unchecked;

        var rootFolders = new List<OneDriveFolderNode> { root };

        var selectedFolders = _service.GetSelectedFolders(rootFolders);

        selectedFolders.ShouldBeEmpty();
    }

    [Fact]
    public void ClearAllSelectionsRecursively()
    {
        var root = CreateFolder("root", "Root");
        var child = CreateFolder("child", "Child", "root");

        root.Children.Add(child);

        root.SelectionState = SelectionState.Checked;
        child.SelectionState = SelectionState.Checked;

        var rootFolders = new List<OneDriveFolderNode> { root };

        _service.ClearAllSelections(rootFolders);

        root.SelectionState.ShouldBe(SelectionState.Unchecked);
        child.SelectionState.ShouldBe(SelectionState.Unchecked);
        root.IsSelected.ShouldBe(false);
        child.IsSelected.ShouldBe(false);
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSetSelectionFolderIsNull()
    {
        Exception? exception = Record.Exception(() => _service.SetSelection(null!, true));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenUpdateParentStatesFolderIsNull()
    {
        var rootFolders = new List<OneDriveFolderNode>();

        Exception? exception = Record.Exception(() => _service.UpdateParentStates(null!, rootFolders));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenUpdateParentStatesRootFoldersIsNull()
    {
        var folder = CreateFolder("test", "Test");

        Exception? exception = Record.Exception(() => _service.UpdateParentStates(folder, null!));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenGetSelectedFoldersRootFoldersIsNull()
    {
        Exception? exception = Record.Exception(() => _service.GetSelectedFolders(null!));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenClearAllSelectionsRootFoldersIsNull()
    {
        Exception? exception = Record.Exception(() => _service.ClearAllSelections(null!));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenCalculateStateFromChildrenFolderIsNull()
    {
        Exception? exception = Record.Exception(() => _service.CalculateStateFromChildren(null!));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    private static OneDriveFolderNode CreateFolder(string id, string name, string? parentId = null)
    {
        return new OneDriveFolderNode
        {
            Id = id,
            Name = name,
            Path = $"/{name}",  // Add Path property for tests that rely on it
            ParentId = parentId,
            IsFolder = true
        };
    }
}
