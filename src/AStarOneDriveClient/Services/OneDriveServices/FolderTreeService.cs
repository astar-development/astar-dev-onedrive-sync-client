using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Models;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Services.OneDriveServices;

/// <summary>
/// Service for retrieving and managing OneDrive folder hierarchies.
/// </summary>
public sealed class FolderTreeService : IFolderTreeService
{
    private readonly IGraphApiClient _graphApiClient;
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderTreeService"/> class.
    /// </summary>
    /// <param name="graphApiClient">The Graph API client.</param>
    /// <param name="authService">The authentication service.</param>
    public FolderTreeService(IGraphApiClient graphApiClient, IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(graphApiClient);
        ArgumentNullException.ThrowIfNull(authService);
        _graphApiClient = graphApiClient;
        _authService = authService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        // Verify account is authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if (!isAuthenticated)
        {
            return Array.Empty<OneDriveFolderNode>();
        }

        var driveItems = await _graphApiClient.GetRootChildrenAsync(accountId, cancellationToken);
        var folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach (var item in folders)
        {
            if (item.Id is null || item.Name is null) continue;

            var node = new OneDriveFolderNode(
                id: item.Id,
                name: item.Name,
                path: $"/{item.Name}",
                parentId: null,
                isFolder: true);

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
        }

        return nodes;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(string accountId, string parentFolderId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(parentFolderId);

        // Verify account is authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if (!isAuthenticated)
        {
            return Array.Empty<OneDriveFolderNode>();
        }

        // Get parent folder to build paths
        var parentItem = await _graphApiClient.GetDriveItemAsync(accountId, parentFolderId, cancellationToken);
        var parentPath = parentItem?.ParentReference?.Path is not null
            ? $"{parentItem.ParentReference.Path}/{parentItem.Name}"
            : $"/{parentItem?.Name}";

        var driveItems = await _graphApiClient.GetDriveItemChildrenAsync(accountId, parentFolderId, cancellationToken);
        var folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach (var item in folders)
        {
            if (item.Id is null || item.Name is null) continue;

            var node = new OneDriveFolderNode(
                id: item.Id,
                name: item.Name,
                path: $"{parentPath}/{item.Name}",
                parentId: parentFolderId,
                isFolder: true);

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
        }

        return nodes;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetFolderHierarchyAsync(string accountId, int? maxDepth = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        // Verify account is authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if (!isAuthenticated)
        {
            return Array.Empty<OneDriveFolderNode>();
        }

        var rootFolders = await GetRootFoldersAsync(accountId, cancellationToken);
        var rootList = rootFolders.ToList();

        if (maxDepth is null || maxDepth > 0)
        {
            foreach (var folder in rootList)
            {
                await LoadChildrenRecursiveAsync(accountId, folder, maxDepth, 1, cancellationToken);
            }
        }

        return rootList;
    }

    private async Task LoadChildrenRecursiveAsync(
        string accountId,
        OneDriveFolderNode parentNode,
        int? maxDepth,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if (maxDepth.HasValue && currentDepth >= maxDepth.Value)
        {
            return;
        }

        var children = await GetChildFoldersAsync(accountId, parentNode.Id, cancellationToken);
        foreach (var child in children)
        {
            parentNode.Children.Add(child);
            await LoadChildrenRecursiveAsync(accountId, child, maxDepth, currentDepth + 1, cancellationToken);
        }

        parentNode.ChildrenLoaded = true;
    }
}
