using System.Collections.ObjectModel;

namespace AStarOneDriveClient.Models;

/// <summary>
/// Represents a folder or file node in the OneDrive folder hierarchy.
/// </summary>
/// <remarks>
/// This model is used for building the folder tree UI and managing sync selection.
/// The Children collection supports lazy loading for efficient tree rendering.
/// </remarks>
public sealed class OneDriveFolderNode
{
    /// <summary>
    /// Gets or sets the unique identifier for this item (OneDrive DriveItem ID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the folder or file.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path from the OneDrive root.
    /// </summary>
    /// <remarks>
    /// Example: "/Documents/Work/Projects"
    /// </remarks>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent node's ID, or null if this is a root-level item.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node represents a folder (true) or file (false).
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    /// <remarks>
    /// This collection supports lazy loading - children are populated when the node is expanded in the UI.
    /// </remarks>
    public ObservableCollection<OneDriveFolderNode> Children { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether child nodes have been loaded from the API.
    /// </summary>
    /// <remarks>
    /// Used to prevent redundant API calls when expanding/collapsing tree nodes.
    /// </remarks>
    public bool ChildrenLoaded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is expanded in the tree view.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveFolderNode"/> class.
    /// </summary>
    public OneDriveFolderNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveFolderNode"/> class with specified properties.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="path">The full path.</param>
    /// <param name="parentId">The parent node ID.</param>
    /// <param name="isFolder">Whether this is a folder.</param>
    public OneDriveFolderNode(string id, string name, string path, string? parentId, bool isFolder)
    {
        Id = id;
        Name = name;
        Path = path;
        ParentId = parentId;
        IsFolder = isFolder;
    }
}
