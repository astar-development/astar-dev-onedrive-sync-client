using System.Text.RegularExpressions;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for managing folder selection state in the sync tree.
/// </summary>
public sealed class SyncSelectionService(ISyncConfigurationRepository configurationRepository) : ISyncSelectionService
{
    /// <inheritdoc />
    public void SetSelection(OneDriveFolderNode folder, bool isSelected)
    {
        SelectionState selectionState = isSelected ? SelectionState.Checked : SelectionState.Unchecked;
        folder.SelectionState = selectionState;
        folder.IsSelected = isSelected;

        CascadeSelectionToChildren(folder, selectionState);
    }

    /// <inheritdoc />
    public void UpdateParentStates(OneDriveFolderNode folder, List<OneDriveFolderNode> rootFolders)
    {
        if(folder.ParentId is null)
            return;

        OneDriveFolderNode? parent = FindNodeById(rootFolders, folder.ParentId);
        if(parent is null)
            return;

        SelectionState calculatedState = CalculateStateFromChildren(parent);
        parent.SelectionState = calculatedState;
        parent.IsSelected = calculatedState switch
        {
            SelectionState.Checked => true,
            SelectionState.Unchecked => false,
            SelectionState.Indeterminate => null,
            _ => null
        };

        UpdateParentStates(parent, rootFolders);
    }

    /// <inheritdoc />
    public List<OneDriveFolderNode> GetSelectedFolders(List<OneDriveFolderNode> rootFolders)
    {
        var selectedFolders = new List<OneDriveFolderNode>();
        CollectSelectedFolders(rootFolders, selectedFolders);
        return selectedFolders;
    }

    /// <inheritdoc />
    public void ClearAllSelections(List<OneDriveFolderNode> rootFolders)
    {
        foreach(OneDriveFolderNode folder in rootFolders)
            SetSelection(folder, false);
    }

    /// <inheritdoc />
    public SelectionState CalculateStateFromChildren(OneDriveFolderNode folder)
    {
        if(folder.Children.Count == 0)
            return folder.SelectionState;

        var checkedCount = 0;
        var uncheckedCount = 0;
        var indeterminateCount = 0;

        foreach(OneDriveFolderNode child in folder.Children)
        {
            switch(child.SelectionState)
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

        return indeterminateCount > 0
            ? SelectionState.Indeterminate
            : checkedCount == folder.Children.Count
            ? SelectionState.Checked
            : uncheckedCount == folder.Children.Count && folder.IsSelected == false
            ? SelectionState.Unchecked
            : SelectionState.Indeterminate;
    }

    /// <inheritdoc />
    public async Task SaveSelectionsToDatabaseAsync(HashedAccountId hashedAccountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default)
    {
        IEnumerable<FileMetadata> configurations = rootFolders.Select(folder => new FileMetadata(folder.DriveItemId, hashedAccountId, folder.Name, folder.Path, 0, DateTime.UtcNow, "", true, false, folder.IsSelected ?? false));

        _ = await configurationRepository.UpdateFoldersByAccountIdAsync(hashedAccountId, configurations, cancellationToken);
    }

    /// <inheritdoc />
    public async Task LoadSelectionsFromDatabaseAsync(HashedAccountId hashedAccountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default)
    {
        DebugLogContext.SetAccountId(hashedAccountId);

        IReadOnlyList<string> savedFolderPaths = await configurationRepository.GetSelectedFoldersAsync(hashedAccountId, cancellationToken);

        _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Loading selections for account {hashedAccountId}", cancellationToken);
        _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Found {savedFolderPaths.Count} saved paths in database", cancellationToken);

        var normalizedSavedPaths = savedFolderPaths
            .Select(NormalizePathForComparison)
            .ToList();

        _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, "Normalized paths:", cancellationToken);
        foreach(var path in normalizedSavedPaths)
            _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Normalized: {path}", cancellationToken);

        var pathToNodeMap = new Dictionary<string, OneDriveFolderNode>(StringComparer.OrdinalIgnoreCase);
        BuildPathLookup(rootFolders, pathToNodeMap);

        var normalizedPathToNodeMap = new Dictionary<string, OneDriveFolderNode>(StringComparer.OrdinalIgnoreCase);
        foreach(KeyValuePair<string, OneDriveFolderNode> kvp in pathToNodeMap)
        {
            var normalized = NormalizePathForComparison(kvp.Key);
            if(!normalizedPathToNodeMap.ContainsKey(normalized))
                normalizedPathToNodeMap[normalized] = kvp.Value;
        }

        foreach(OneDriveFolderNode folder in pathToNodeMap.Values)
        {
            folder.SelectionState = SelectionState.Unchecked;
            folder.IsSelected = false;
        }

        if(savedFolderPaths.Count > 0)
        {
            for(var i = 0; i < savedFolderPaths.Count; i++)
            {
                var normalizedPath = normalizedSavedPaths[i];
                if(normalizedPathToNodeMap.TryGetValue(normalizedPath, out OneDriveFolderNode? folder))
                    SetSelection(folder, true);
            }

            _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, "Checking root folders for selected descendants", cancellationToken);
            foreach(OneDriveFolderNode rootFolder in rootFolders)
            {
                _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Checking root: {rootFolder.Path} (State: {rootFolder.SelectionState})", cancellationToken);

                if(rootFolder.SelectionState == SelectionState.Checked)
                {
                    _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, "Already checked, skipping", cancellationToken);
                    continue;
                }

                var normalizedRootPath = NormalizePathForComparison(rootFolder.Path);
                _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Root normalized to: {normalizedRootPath}", cancellationToken);

                var hasSelectedDescendants = normalizedSavedPaths.Any(path => path.StartsWith(normalizedRootPath + "/", StringComparison.OrdinalIgnoreCase) ||
                                                                              (normalizedRootPath == "/" && path.StartsWith("/", StringComparison.OrdinalIgnoreCase) && path != "/"));

                _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Has selected descendants: {hasSelectedDescendants}", cancellationToken);

                if(hasSelectedDescendants)
                {
                    _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Setting {rootFolder.Path} to Indeterminate", cancellationToken);
                    rootFolder.SelectionState = SelectionState.Indeterminate;
                    rootFolder.IsSelected = null;
                    _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"After setting - State: {rootFolder.SelectionState}, IsSelected: {rootFolder.IsSelected}", cancellationToken);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<IList<OneDriveFolderNode>> LoadSelectionsFromDatabaseAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        DebugLogContext.SetAccountId(hashedAccountId);
        IList<OneDriveFolderNode>folders = [];
        IReadOnlyList<DriveItemEntity> savedFolders = await configurationRepository.GetFoldersByAccountIdAsync(hashedAccountId, cancellationToken);

        _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Loading selections for account {hashedAccountId}", cancellationToken);
        _ = await DebugLog.LogInfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", hashedAccountId, $"Found {savedFolders.Count} saved folders in database", cancellationToken);

        foreach(DriveItemEntity entity in savedFolders)
        {
            var folderNode = new OneDriveFolderNode
            {
                DriveItemId = entity.DriveItemId,
                Name = entity.Name??"",
                Path = entity.RelativePath,
                IsSelected = entity.IsSelected,
                SelectionState = entity.IsSelected ?? false ? SelectionState.Checked : SelectionState.Unchecked
            };

            folders.Add(folderNode);
        }

        return folders;
    }

    /// <inheritdoc />
    public void UpdateParentState(OneDriveFolderNode folder)
    {
        if(folder.Children.Count == 0)
            return;

        SelectionState calculatedState = CalculateStateFromChildren(folder);
        folder.SelectionState = calculatedState;
        folder.IsSelected = calculatedState switch
        {
            SelectionState.Checked => true,
            SelectionState.Unchecked => false,
            SelectionState.Indeterminate => null,
            _ => null
        };
    }

    private static void CascadeSelectionToChildren(OneDriveFolderNode folder, SelectionState state)
    {
        foreach(OneDriveFolderNode child in folder.Children)
        {
            child.SelectionState = state;
            child.IsSelected = state switch
            {
                SelectionState.Checked => true,
                SelectionState.Unchecked => false,
                SelectionState.Indeterminate => throw new NotImplementedException(),
                _ => null
            };

            CascadeSelectionToChildren(child, state);
        }
    }

    private static void CollectSelectedFolders(List<OneDriveFolderNode> folders, List<OneDriveFolderNode> result)
    {
        foreach(OneDriveFolderNode folder in folders)
        {
            if(folder.SelectionState == SelectionState.Checked &&
               !string.IsNullOrEmpty(folder.Path) &&
               !string.IsNullOrEmpty(folder.Name))
            {
                result.Add(folder);
            }

            CollectSelectedFolders([.. folder.Children], result);
        }
    }

    private static OneDriveFolderNode? FindNodeById(List<OneDriveFolderNode> folders, string nodeId)
    {
        foreach(OneDriveFolderNode folder in folders)
        {
            if(folder.DriveItemId == nodeId)
                return folder;

            OneDriveFolderNode? foundInChildren = FindNodeById([.. folder.Children], nodeId);
            if(foundInChildren is not null)
                return foundInChildren;
        }

        return null;
    }

    private static void BuildPathLookup(List<OneDriveFolderNode> folders, Dictionary<string, OneDriveFolderNode> pathMap)
    {
        foreach(OneDriveFolderNode folder in folders)
        {
            if(string.IsNullOrEmpty(folder.Path))
                continue;

            pathMap[folder.Path] = folder;

            if(folder.Children.Count > 0)
                BuildPathLookup([.. folder.Children], pathMap);
        }
    }

    /// <summary>
    ///     Normalizes a path by removing Graph API prefixes for comparison.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path without Graph API prefixes.</returns>
    private static string NormalizePathForComparison(string path)
    {
        if(string.IsNullOrEmpty(path))
            return path;

        var drivesPattern = @"^/drives/[^/]+/root:";
        if(Regex.IsMatch(path, drivesPattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            path = Regex.Replace(path, drivesPattern, string.Empty, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));

        if(path.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
            path = path["/drive/root:".Length..];

        if(!path.StartsWith('/'))
            path = "/" + path;

        return path;
    }

    private void RecalculateParentStates(OneDriveFolderNode folder)
    {
        if(folder.Children.Count > 0)
        {
            foreach(OneDriveFolderNode child in folder.Children)
                RecalculateParentStates(child);

            SelectionState calculatedState = CalculateStateFromChildren(folder);
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
