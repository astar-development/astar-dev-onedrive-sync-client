using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Services.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Services;

/// <summary>
/// Service for detecting changes on OneDrive using delta queries.
/// </summary>
/// <remarks>
/// Note: Full delta query support requires Microsoft.Graph SDK capabilities.
/// This implementation provides a foundation that can be extended when delta APIs are integrated.
/// For now, it performs full scans and compares against known state.
/// </remarks>
public sealed class RemoteChangeDetector : IRemoteChangeDetector
{
    private readonly IGraphApiClient _graphApiClient;

    public RemoteChangeDetector(IGraphApiClient graphApiClient)
    {
        ArgumentNullException.ThrowIfNull(graphApiClient);
        _graphApiClient = graphApiClient;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)> DetectChangesAsync(
        string accountId,
        string folderPath,
        string? previousDeltaLink,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(folderPath);

        cancellationToken.ThrowIfCancellationRequested();

        // For initial implementation, scan the folder tree
        // Note: For large OneDrive accounts (100k+ files), this can take several minutes
        // In future sprints, this will be enhanced with proper delta query support
        var changes = new List<FileMetadata>();
        var rootItem = await GetFolderItemAsync(accountId, folderPath, cancellationToken);

        if (rootItem is not null)
        {
            // Add a practical limit for initial scan to prevent timeout
            // This will be removed when we implement proper delta query support
            const int maxFiles = 10000; // Limit initial scan to 10k files
            await ScanFolderRecursiveAsync(accountId, rootItem, folderPath, changes, cancellationToken, maxFiles);
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Scan complete: {changes.Count} files found in {folderPath}");
        }

        // Generate a simple delta token based on timestamp
        // In production, this would be the actual deltaLink from Graph API
        var newDeltaLink = $"delta_{DateTime.UtcNow:yyyyMMddHHmmss}";

        return (changes, newDeltaLink);
    }

    private async Task<DriveItem?> GetFolderItemAsync(string accountId, string folderPath, CancellationToken cancellationToken)
    {
        // For root or empty path, return the drive root
        if (folderPath == "/" || string.IsNullOrEmpty(folderPath))
        {
            return await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        }

        // Clean up the path
        folderPath = folderPath.Trim('/');

        // Get root and traverse path segments
        var currentItem = await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        if (currentItem?.Id is null)
        {
            return null;
        }

        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in pathSegments)
        {
            var children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, currentItem.Id, cancellationToken);

            currentItem = children.FirstOrDefault(c =>
                c.Name?.Equals(segment, StringComparison.OrdinalIgnoreCase) == true &&
                c.Folder is not null);

            if (currentItem?.Id is null)
            {
                return null;
            }
        }

        return currentItem;
    }

    private async Task ScanFolderRecursiveAsync(
        string accountId,
        DriveItem parentItem,
        string currentPath,
        List<FileMetadata> changes,
        CancellationToken cancellationToken,
        int maxFiles = int.MaxValue)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (changes.Count >= maxFiles)
        {
            return; // Reached the limit
        }

        if (parentItem.Id is null)
        {
            return;
        }

        var children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, parentItem.Id, cancellationToken);

        foreach (var item in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (changes.Count >= maxFiles)
            {
                return; // Reached the limit
            }

            if (item.File is not null && item.Id is not null && item.Name is not null)
            {
                // It's a file
                var itemPath = CombinePaths(currentPath, item.Name);
                var metadata = ConvertToFileMetadata(accountId, item, itemPath);
                changes.Add(metadata);
                if (changes.Count % 500 == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Progress: {changes.Count} files scanned");
                }
            }
            else if (item.Folder is not null && item.Id is not null && item.Name is not null)
            {
                // It's a folder - scan recursively
                var itemPath = CombinePaths(currentPath, item.Name);
                await ScanFolderRecursiveAsync(accountId, item, itemPath, changes, cancellationToken, maxFiles);
            }
        }
    }

    private static FileMetadata ConvertToFileMetadata(string accountId, DriveItem item, string path)
    {
        return new FileMetadata(
            Id: item.Id ?? string.Empty,
            AccountId: accountId,
            Name: item.Name ?? string.Empty,
            Path: path,
            Size: item.Size ?? 0,
            LastModifiedUtc: item.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
            LocalPath: string.Empty, // Will be set during download
            CTag: item.CTag,
            ETag: item.ETag,
            LocalHash: null, // Will be computed after download
            SyncStatus: FileSyncStatus.PendingDownload,
            LastSyncDirection: SyncDirection.Download);
    }

    private static string CombinePaths(string basePath, string name)
    {
        basePath = basePath.Replace('\\', '/');
        name = name.Replace('\\', '/');

        if (!basePath.EndsWith('/'))
        {
            basePath += '/';
        }

        if (name.StartsWith('/'))
        {
            name = name[1..];
        }

        return basePath + name;
    }
}
