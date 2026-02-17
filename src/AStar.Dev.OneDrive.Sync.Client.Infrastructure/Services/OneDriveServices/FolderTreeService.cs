using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;

/// <summary>
///     Service for retrieving and managing OneDrive folder hierarchies.
/// </summary>
public sealed class FolderTreeService(IGraphApiClient graphApiClient, IAuthService authService, ISyncConfigurationRepository syncConfigurationRepository) : IFolderTreeService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
            return [];

        IEnumerable<DriveItem> driveItems = await graphApiClient.GetRootChildrenAsync(accountId, hashedAccountId, cancellationToken);
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
                true)
            { IsSelected = false };

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
            var possibleParentPath = SyncEngine.FormatScanningFolderForDisplay(item.Name)!.Replace("OneDrive: ", string.Empty);
            FileMetadata configuration = await UpdateParentPathIfExistsAsync(hashedAccountId, node, possibleParentPath, cancellationToken);

            _ = await syncConfigurationRepository.AddAsync(configuration, cancellationToken);
        }

        return nodes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(string accountId, HashedAccountId hashedAccountId, string parentFolderId, bool? parentIsSelected = null, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
            return [];

        DriveItem? parentItem = await graphApiClient.GetDriveItemAsync(accountId, hashedAccountId, parentFolderId, cancellationToken);
        var parentPath = parentItem?.ParentReference?.Path is not null
            ? $"{parentItem.ParentReference.Path}/{parentItem.Name}"
            : $"/{parentItem?.Name}";

        IEnumerable<DriveItem> driveItems = await graphApiClient.GetDriveItemChildrenAsync(accountId, hashedAccountId, parentFolderId, cancellationToken);
        IEnumerable<DriveItem> folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach(DriveItem? item in folders)
        {
            if(item.Id is null || item.Name is null)
                continue;

            var node = new OneDriveFolderNode(item.Id,item.Name,$"{parentPath}/{item.Name}",parentFolderId,true);

            var possibleParentPath = SyncEngine.FormatScanningFolderForDisplay(item.Name)!.Replace("OneDrive: ", string.Empty);
            FileMetadata updatedSyncConfiguration = await UpdateParentPathIfExistsAsync(hashedAccountId, node, possibleParentPath, cancellationToken);
            var isSelected = parentIsSelected == true || updatedSyncConfiguration.IsSelected;

            node = new OneDriveFolderNode(item.Id,item.Name,$"{parentPath}/{item.Name}",parentFolderId,true)
            { IsSelected = isSelected };

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
        }

        return nodes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetFolderHierarchyAsync(string accountId, HashedAccountId hashedAccountId, int? maxDepth = null, CancellationToken cancellationToken = default)
    {
        var isAuthenticated = await authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
            return [];

        IReadOnlyList<OneDriveFolderNode> rootFolders = await GetRootFoldersAsync(accountId, hashedAccountId, cancellationToken);
        var rootList = rootFolders.ToList();

        if(maxDepth is not (null or > 0))
            return rootList;
        foreach(OneDriveFolderNode? folder in rootList)
            await LoadChildrenRecursiveAsync(accountId, hashedAccountId, folder, maxDepth, 1, cancellationToken);

        return rootList;
    }

    private async Task<FileMetadata> UpdateParentPathIfExistsAsync(HashedAccountId hashedAccountId, OneDriveFolderNode node, string possibleParentPath, CancellationToken cancellationToken)
    {
        var configuration = new FileMetadata(node.DriveItemId, hashedAccountId, node.Name, node.Path, 0, DateTime.UtcNow, "", true, false, true);

        var lastIndexOf = node.Path.LastIndexOf('/');
        if(lastIndexOf > 0)
        {
            var parentPath = configuration.RelativePath[..lastIndexOf];
            DriveItemEntity? parentEntity = await syncConfigurationRepository.GetParentFolderAsync(hashedAccountId, parentPath, possibleParentPath, cancellationToken);

            if(parentEntity is not null)
            {
                var updatedPath = SyncEngine.FormatScanningFolderForDisplay(configuration.RelativePath)!.Replace("OneDrive: ", string.Empty);
                configuration = configuration with { RelativePath = updatedPath, IsSelected = parentEntity.IsSelected ?? false };
            }
        }

        return configuration;
    }

    private async Task LoadChildrenRecursiveAsync(string accountId,HashedAccountId hashedAccountId,OneDriveFolderNode parentNode,int? maxDepth,int currentDepth,CancellationToken cancellationToken)
    {
        if(maxDepth.HasValue && currentDepth >= maxDepth.Value)
            return;

        IReadOnlyList<OneDriveFolderNode> children = await GetChildFoldersAsync(accountId, hashedAccountId, parentNode.DriveItemId, parentNode.IsSelected, cancellationToken);
        foreach(OneDriveFolderNode child in children)
        {
            parentNode.Children.Add(child);
            await LoadChildrenRecursiveAsync(accountId, hashedAccountId, child, maxDepth, currentDepth + 1, cancellationToken);
        }

        parentNode.ChildrenLoaded = true;
    }
}
