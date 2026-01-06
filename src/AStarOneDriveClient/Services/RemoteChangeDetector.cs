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

        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Scanning folder: {folderPath}");

        // For initial implementation, scan the folder tree
        // Note: For large OneDrive accounts (100k+ files), this can take several minutes
        // In future sprints, this will be enhanced with proper delta query support
        var changes = new List<FileMetadata>();
        var rootItem = await GetFolderItemAsync(accountId, folderPath, cancellationToken);

        if (rootItem is not null)
        {
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Found folder item: {rootItem.Name}, ID: {rootItem.Id}");
            // Add a practical limit for initial scan to prevent timeout
            // This will be removed when we implement proper delta query support
            const int maxFiles = 10000; // Limit initial scan to 10k files
            await ScanFolderRecursiveAsync(accountId, rootItem, folderPath, changes, cancellationToken, maxFiles);
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Scan complete: {changes.Count} files found in {folderPath}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] WARNING: Could not find folder item for path: {folderPath}");
        }

        // Generate a simple delta token based on timestamp
        // In production, this would be the actual deltaLink from Graph API
        var newDeltaLink = $"delta_{DateTime.UtcNow:yyyyMMddHHmmss}";

        return (changes, newDeltaLink);
    }

    private async Task<DriveItem?> GetFolderItemAsync(string accountId, string folderPath, CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] GetFolderItemAsync called with path: '{folderPath}'");

        // For root or empty path, return the drive root
        if (folderPath == "/" || string.IsNullOrEmpty(folderPath))
        {
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Returning drive root for path: '{folderPath}'");
            return await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        }

        // Clean up the path
        folderPath = folderPath.Trim('/');
        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Cleaned path: '{folderPath}'");

        // Get root and traverse path segments
        var currentItem = await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        if (currentItem?.Id is null)
        {
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] ERROR: Could not get drive root");
            return null;
        }

        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Got drive root: {currentItem.Name}, traversing path segments...");
        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Path segments: {string.Join(" -> ", pathSegments)}");

        foreach (var segment in pathSegments)
        {
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Looking for folder segment: '{segment}' in item: {currentItem.Name}");
            var children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, currentItem.Id, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Found {children.Count()} children");

            currentItem = children.FirstOrDefault(c =>
                c.Name?.Equals(segment, StringComparison.OrdinalIgnoreCase) == true &&
                c.Folder is not null);

            if (currentItem?.Id is null)
            {
                System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] ERROR: Could not find folder segment '{segment}' in path");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Found segment: {currentItem.Name}");
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
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Reached max files limit: {maxFiles}");
            return; // Reached the limit
        }

        if (parentItem.Id is null)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Scanning folder: {currentPath}, Files so far: {changes.Count}");
        var children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, parentItem.Id, cancellationToken);
        System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Found {children.Count()} items in {currentPath}");

        foreach (var item in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (changes.Count >= maxFiles)
            {
                System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Reached max files limit: {maxFiles}");
                return; // Reached the limit
            }

            if (item.File is not null && item.Id is not null && item.Name is not null)
            {
                // It's a file
                var itemPath = CombinePaths(currentPath, item.Name);
                var metadata = ConvertToFileMetadata(accountId, item, itemPath);
                changes.Add(metadata);
                if (changes.Count % 100 == 0)
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
