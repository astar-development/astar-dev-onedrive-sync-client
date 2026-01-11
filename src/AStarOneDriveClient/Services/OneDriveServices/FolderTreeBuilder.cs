using AStarOneDriveClient.Models;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Services.OneDriveServices;

/// <summary>
/// Helper class for building hierarchical folder trees from flat DriveItem collections.
/// </summary>
public static class FolderTreeBuilder
{
    /// <summary>
    /// Builds a hierarchical tree structure from a flat list of DriveItems.
    /// </summary>
    /// <param name="items">Flat list of DriveItems to organize into a tree.</param>
    /// <param name="rootParentId">The parent ID to consider as root level. Items with this parent ID will be at the top level. Use null for actual root items.</param>
    /// <returns>List of root-level OneDriveFolderNode objects with their children populated.</returns>
    /// <remarks>
    /// This method is useful for performance optimization when you have a batch of items
    /// from the Graph API and want to organize them hierarchically without additional API calls.
    /// Only folder items (where Folder property is not null) are included in the tree.
    /// </remarks>
    public static List<OneDriveFolderNode> BuildTree(IEnumerable<DriveItem> items, string? rootParentId = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemsList = items.ToList();
        var folderItems = itemsList.Where(item => item.Folder is not null).ToList();

        // Create dictionary for quick lookup by ID
        var nodeDict = new Dictionary<string, OneDriveFolderNode>();

        // First pass: Create all nodes
        foreach (DriveItem? item in folderItems)
        {
            if (item.Id is null || item.Name is null)
            {
                continue;
            }

            var path = BuildPath(item);
            var parentId = item.ParentReference?.Id;

            var node = new OneDriveFolderNode
            {
                Id = item.Id,
                Name = item.Name,
                Path = path,
                ParentId = parentId,
                IsFolder = true
            };

            nodeDict[item.Id] = node;
        }

        // Second pass: Build parent-child relationships
        var rootNodes = new List<OneDriveFolderNode>();

        foreach (OneDriveFolderNode node in nodeDict.Values)
        {
            if (node.ParentId == rootParentId)
            {
                // This is a root-level node
                rootNodes.Add(node);
            }
            else if (node.ParentId is not null && nodeDict.TryGetValue(node.ParentId, out OneDriveFolderNode? parentNode))
            {
                // Add this node as a child of its parent
                parentNode.Children.Add(node);
            }
        }

        // Sort children by name for consistent ordering
        foreach (OneDriveFolderNode node in nodeDict.Values)
        {
            if (node.Children.Count > 0)
            {
                var sortedChildren = node.Children.OrderBy(c => c.Name).ToList();
                node.Children.Clear();
                foreach (OneDriveFolderNode? child in sortedChildren)
                {
                    node.Children.Add(child);
                }
            }
        }

        return [.. rootNodes.OrderBy(n => n.Name)];
    }

    /// <summary>
    /// Builds the full path for a DriveItem based on its ParentReference.
    /// </summary>
    /// <param name="item">The DriveItem to build a path for.</param>
    /// <returns>The full path, or just the item name prefixed with "/" if path cannot be determined.</returns>
    private static string BuildPath(DriveItem item)
    {
        if (item.ParentReference?.Path is not null && item.Name is not null)
        {
            // Path format from Graph API can be:
            // - "/drive/root:/Documents/Work" (personal OneDrive)
            // - "/drives/{drive-id}/root:/Documents/Work" (shared drives or when accessing via drive ID)
            // We want: "/Documents/Work/FolderName"
            var parentPath = item.ParentReference.Path;

            // Remove the "/drive/root:" or "/drives/{drive-id}/root:" prefix if present
            if (parentPath.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
            {
                parentPath = parentPath["/drive/root:".Length..];
            }
            else if (parentPath.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
            {
                // Find the "/root:" part and remove everything up to and including it
                var rootIndex = parentPath.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
                if (rootIndex >= 0)
                {
                    parentPath = parentPath[(rootIndex + "/root:".Length)..];
                }
            }

            // If parent is root, path is just "/FolderName"
            if (string.IsNullOrEmpty(parentPath) || parentPath == "/")
            {
                return $"/{item.Name}";
            }

            // Otherwise append the folder name
            return $"{parentPath}/{item.Name}";
        }

        // Fallback: just use the name
        return $"/{item.Name ?? "Unknown"}";
    }

    /// <summary>
    /// Merges new items into an existing tree structure.
    /// </summary>
    /// <param name="existingTree">The existing tree to merge into.</param>
    /// <param name="newItems">New DriveItems to add to the tree.</param>
    /// <remarks>
    /// This method is useful when lazy-loading children for a specific folder.
    /// It will find the parent node in the existing tree and add the new items as children.
    /// </remarks>
    public static void MergeIntoTree(List<OneDriveFolderNode> existingTree, IEnumerable<DriveItem> newItems, string parentId)
    {
        ArgumentNullException.ThrowIfNull(existingTree);
        ArgumentNullException.ThrowIfNull(newItems);
        ArgumentNullException.ThrowIfNull(parentId);

        OneDriveFolderNode? parentNode = FindNodeById(existingTree, parentId);
        if (parentNode is null)
        {
            return;
        }

        List<OneDriveFolderNode> newNodes = BuildTree(newItems, parentId);

        foreach (OneDriveFolderNode newNode in newNodes)
        {
            // Check if this node already exists
            if (!parentNode.Children.Any(c => c.Id == newNode.Id))
            {
                parentNode.Children.Add(newNode);
            }
        }

        // Sort children after adding
        if (parentNode.Children.Count > 0)
        {
            var sortedChildren = parentNode.Children.OrderBy(c => c.Name).ToList();
            parentNode.Children.Clear();
            foreach (OneDriveFolderNode? child in sortedChildren)
            {
                parentNode.Children.Add(child);
            }
        }

        parentNode.ChildrenLoaded = true;
    }

    /// <summary>
    /// Finds a node by ID in the tree structure (recursive search).
    /// </summary>
    /// <param name="tree">The tree to search in.</param>
    /// <param name="nodeId">The ID of the node to find.</param>
    /// <returns>The node if found, otherwise null.</returns>
    private static OneDriveFolderNode? FindNodeById(List<OneDriveFolderNode> tree, string nodeId)
    {
        foreach (OneDriveFolderNode node in tree)
        {
            if (node.Id == nodeId)
            {
                return node;
            }

            OneDriveFolderNode? foundInChildren = FindNodeById([.. node.Children], nodeId);
            if (foundInChildren is not null)
            {
                return foundInChildren;
            }
        }

        return null;
    }
}
