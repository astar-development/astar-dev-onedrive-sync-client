using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using AStar.Dev.OneDrive.Client.Models;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.Services.OneDriveServices;

/// <summary>
///     Service for retrieving and managing OneDrive folder hierarchies.
/// </summary>
public sealed class FolderTreeService(IGraphApiClient graphApiClient, IAuthService authService, ISyncConfigurationRepository syncConfigurationRepository) : IFolderTreeService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
            return [];

        IEnumerable<DriveItem> driveItems = await graphApiClient.GetRootChildrenAsync(accountId, cancellationToken);
        IEnumerable<DriveItem> folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach(DriveItem? item in folders)
        {
            if(item.Id is null || item.Name is null)
                continue;

            var node = new OneDriveFolderNode(
                item.Id,
                item.Name,
                $"/{item.Name}",
                item.ParentReference?.Id,
                true) { IsSelected = false };

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
            var possibleParentPath = SyncEngine.FormatScanningFolderForDisplay(item.Name)!.Replace("OneDrive: ", string.Empty);
            SyncConfiguration configuration = await UpdateParentPathIfExistsAsync(accountId, node, possibleParentPath, cancellationToken);

            _ = await syncConfigurationRepository.AddAsync(configuration, cancellationToken);
        }

        return nodes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(string accountId, string parentFolderId, bool? parentIsSelected = null, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
            return [];

        DriveItem? parentItem = await graphApiClient.GetDriveItemAsync(accountId, parentFolderId, cancellationToken);
        var parentPath = parentItem?.ParentReference?.Path is not null
            ? $"{parentItem.ParentReference.Path}/{parentItem.Name}"
            : $"/{parentItem?.Name}";

        IEnumerable<DriveItem> driveItems = await graphApiClient.GetDriveItemChildrenAsync(accountId, parentFolderId, cancellationToken);
        IEnumerable<DriveItem> folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach(DriveItem? item in folders)
        {
            if(item.Id is null || item.Name is null)
                continue;

            var node = new OneDriveFolderNode(
                item.Id,
                item.Name,
                $"{parentPath}/{item.Name}",
                parentFolderId,
                true);

            var possibleParentPath = SyncEngine.FormatScanningFolderForDisplay(item.Name)!.Replace("OneDrive: ", string.Empty);
            SyncConfiguration updatedSyncConfiguration = await UpdateParentPathIfExistsAsync(accountId, node, possibleParentPath, cancellationToken);
            bool? isSelected = parentIsSelected == true || updatedSyncConfiguration.IsSelected;

            node = new OneDriveFolderNode(
                item.Id,
                item.Name,
                $"{parentPath}/{item.Name}",
                parentFolderId,
                true) { IsSelected = isSelected };

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
        }

        return nodes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetFolderHierarchyAsync(string accountId, int? maxDepth = null, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
            return [];

        IReadOnlyList<OneDriveFolderNode> rootFolders = await GetRootFoldersAsync(accountId, cancellationToken);
        var rootList = rootFolders.ToList();

        if(maxDepth is not (null or > 0))
            return rootList;
        foreach(OneDriveFolderNode? folder in rootList)
            await LoadChildrenRecursiveAsync(accountId, folder, maxDepth, 1, cancellationToken);

        return rootList;
    }

    private async Task<SyncConfiguration> UpdateParentPathIfExistsAsync(string accountId, OneDriveFolderNode node, string possibleParentPath, CancellationToken cancellationToken)
    {
        var configuration = new SyncConfiguration(0, accountId, node.Path, false, DateTime.UtcNow);

        var lastIndexOf = configuration.FolderPath.LastIndexOf('/');
        if(lastIndexOf > 0)
        {
            var parentPath = configuration.FolderPath[..lastIndexOf];
            SyncConfigurationEntity? parentEntity = await syncConfigurationRepository.GetParentFolderAsync(accountId, parentPath, possibleParentPath, cancellationToken);

            if(parentEntity is not null)
            {
                var updatedPath = SyncEngine.FormatScanningFolderForDisplay(configuration.FolderPath)!.Replace("OneDrive: ", string.Empty);
                configuration = configuration with { FolderPath = updatedPath, IsSelected = parentEntity.IsSelected };
            }
        }

        return configuration;
    }

    private async Task LoadChildrenRecursiveAsync(
        string accountId,
        OneDriveFolderNode parentNode,
        int? maxDepth,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if(maxDepth.HasValue && currentDepth >= maxDepth.Value)
            return;

        IReadOnlyList<OneDriveFolderNode> children = await GetChildFoldersAsync(accountId, parentNode.Id, parentNode.IsSelected, cancellationToken);
        foreach(OneDriveFolderNode child in children)
        {
            parentNode.Children.Add(child);
            await LoadChildrenRecursiveAsync(accountId, child, maxDepth, currentDepth + 1, cancellationToken);
        }

        parentNode.ChildrenLoaded = true;
    }
}
