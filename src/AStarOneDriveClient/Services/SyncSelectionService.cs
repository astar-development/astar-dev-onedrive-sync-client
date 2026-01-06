using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;

namespace AStarOneDriveClient.Services;

/// <summary>
/// Service for managing folder selection state in the sync tree.
/// </summary>
public sealed class SyncSelectionService : ISyncSelectionService
{
    private readonly ISyncConfigurationRepository? _configurationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSelectionService"/> class.
    /// </summary>
    public SyncSelectionService()
    {
        // Parameterless constructor for backward compatibility with existing tests
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSelectionService"/> class with database persistence.
    /// </summary>
    /// <param name="configurationRepository">The sync configuration repository.</param>
    public SyncSelectionService(ISyncConfigurationRepository configurationRepository)
    {
        _configurationRepository = configurationRepository;
    }
    /// <inheritdoc/>
    public void SetSelection(OneDriveFolderNode folder, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var selectionState = isSelected ? SelectionState.Checked : SelectionState.Unchecked;
        folder.SelectionState = selectionState;
        folder.IsSelected = isSelected;

        // Cascade to all children
        CascadeSelectionToChildren(folder, selectionState);
    }

    /// <inheritdoc/>
    public void UpdateParentStates(OneDriveFolderNode folder, List<OneDriveFolderNode> rootFolders)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(rootFolders);

        if (folder.ParentId is null)
            return;

        var parent = FindNodeById(rootFolders, folder.ParentId);
        if (parent is null)
            return;

        var calculatedState = CalculateStateFromChildren(parent);
        parent.SelectionState = calculatedState;
        parent.IsSelected = calculatedState switch
        {
            SelectionState.Checked => true,
            SelectionState.Unchecked => false,
            SelectionState.Indeterminate => null,
            _ => null
        };

        // Continue propagating upward
        UpdateParentStates(parent, rootFolders);
    }

    /// <inheritdoc/>
    public List<OneDriveFolderNode> GetSelectedFolders(List<OneDriveFolderNode> rootFolders)
    {
        ArgumentNullException.ThrowIfNull(rootFolders);

        var selectedFolders = new List<OneDriveFolderNode>();
        CollectSelectedFolders(rootFolders, selectedFolders);
        return selectedFolders;
    }

    /// <inheritdoc/>
    public void ClearAllSelections(List<OneDriveFolderNode> rootFolders)
    {
        ArgumentNullException.ThrowIfNull(rootFolders);

        foreach (var folder in rootFolders)
        {
            SetSelection(folder, false);
        }
    }

    /// <inheritdoc/>
    public SelectionState CalculateStateFromChildren(OneDriveFolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if (folder.Children.Count == 0)
            return folder.SelectionState;

        var checkedCount = 0;
        var uncheckedCount = 0;
        var indeterminateCount = 0;

        foreach (var child in folder.Children)
        {
            switch (child.SelectionState)
            {
                case SelectionState.Checked:
                    checkedCount++;
                    break;
                case SelectionState.Unchecked:
                    uncheckedCount++;
                    break;
                case SelectionState.Indeterminate:
                    indeterminateCount++;
                    break;
            }
        }

        // If any child is indeterminate, parent is indeterminate
        if (indeterminateCount > 0)
            return SelectionState.Indeterminate;

        // If all children are checked, parent is checked
        if (checkedCount == folder.Children.Count)
            return SelectionState.Checked;

        // If all children are unchecked, parent is unchecked
        if (uncheckedCount == folder.Children.Count)
            return SelectionState.Unchecked;

        // Mixed state = indeterminate
        return SelectionState.Indeterminate;
    }

    private static void CascadeSelectionToChildren(OneDriveFolderNode folder, SelectionState state)
    {
        foreach (var child in folder.Children)
        {
            child.SelectionState = state;
            child.IsSelected = state switch
            {
                SelectionState.Checked => true,
                SelectionState.Unchecked => false,
                _ => null
            };

            CascadeSelectionToChildren(child, state);
        }
    }

    private static void CollectSelectedFolders(List<OneDriveFolderNode> folders, List<OneDriveFolderNode> result)
    {
        foreach (var folder in folders)
        {
            if (folder.SelectionState == SelectionState.Checked &&
                !string.IsNullOrEmpty(folder.Path) &&
                !string.IsNullOrEmpty(folder.Name))
            {
                result.Add(folder);
            }

            CollectSelectedFolders(folder.Children.ToList(), result);
        }
    }

    private static OneDriveFolderNode? FindNodeById(List<OneDriveFolderNode> folders, string nodeId)
    {
        foreach (var folder in folders)
        {
            if (folder.Id == nodeId)
                return folder;

            var foundInChildren = FindNodeById(folder.Children.ToList(), nodeId);
            if (foundInChildren is not null)
                return foundInChildren;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task SaveSelectionsToDatabaseAsync(string accountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(rootFolders);

        if (_configurationRepository is null)
            return; // No persistence configured

        // Get all checked folders
        var selectedFolders = GetSelectedFolders(rootFolders);

        System.Diagnostics.Debug.WriteLine($"[SyncSelectionService] Saving {selectedFolders.Count} folders to database:");
        foreach (var folder in selectedFolders)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncSelectionService]   - '{folder.Path}' (Name: '{folder.Name}')");
        }

        // Convert to SyncConfiguration records
        var configurations = selectedFolders.Select(folder => new SyncConfiguration(
            Id: 0, // Will be auto-generated
            AccountId: accountId,
            FolderPath: folder.Path,
            IsSelected: true,
            LastModifiedUtc: DateTime.UtcNow
        ));

        // Save batch (replaces all existing selections for this account)
        await _configurationRepository.SaveBatchAsync(accountId, configurations, cancellationToken);
        System.Diagnostics.Debug.WriteLine($"[SyncSelectionService] Save complete");
    }

    /// <inheritdoc/>
    public async Task LoadSelectionsFromDatabaseAsync(string accountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(rootFolders);

        if (_configurationRepository is null)
            return; // No persistence configured

        // Get saved folder paths
        var savedFolderPaths = await _configurationRepository.GetSelectedFoldersAsync(accountId, cancellationToken);

        // Build lookup dictionary for fast path-to-node resolution
        var pathToNodeMap = new Dictionary<string, OneDriveFolderNode>(StringComparer.OrdinalIgnoreCase);
        BuildPathLookup(rootFolders, pathToNodeMap);

        // Initialize ALL folders to Unchecked first
        foreach (var folder in pathToNodeMap.Values)
        {
            folder.SelectionState = SelectionState.Unchecked;
            folder.IsSelected = false;
        }

        // Then set saved selections to Checked
        if (savedFolderPaths.Count > 0)
        {
            foreach (var folderPath in savedFolderPaths)
            {
                if (pathToNodeMap.TryGetValue(folderPath, out var folder))
                {
                    SetSelection(folder, true);
                }
                // Silently ignore folders that no longer exist (deleted or renamed)
            }
        }

        // Recalculate parent states to ensure indeterminate states are correct
        foreach (var rootFolder in rootFolders)
        {
            RecalculateParentStates(rootFolder);
        }
    }

    private static void BuildPathLookup(List<OneDriveFolderNode> folders, Dictionary<string, OneDriveFolderNode> pathMap)
    {
        foreach (var folder in folders)
        {
            pathMap[folder.Path] = folder;

            if (folder.Children.Count > 0)
            {
                BuildPathLookup(folder.Children.ToList(), pathMap);
            }
        }
    }

    private void RecalculateParentStates(OneDriveFolderNode folder)
    {
        if (folder.Children.Count > 0)
        {
            // Recursively update children first
            foreach (var child in folder.Children)
            {
                RecalculateParentStates(child);
            }

            // Then update this folder's state based on children
            var calculatedState = CalculateStateFromChildren(folder);
            folder.SelectionState = calculatedState;
            folder.IsSelected = calculatedState switch
            {
                SelectionState.Checked => true,
                SelectionState.Unchecked => false,
                SelectionState.Indeterminate => null,
                _ => null
            };
        }
    }
}
