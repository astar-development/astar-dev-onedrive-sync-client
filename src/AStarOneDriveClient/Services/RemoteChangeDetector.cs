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
        await DebugLog.EntryAsync("RemoteChangeDetector.DetectChangesAsync", cancellationToken);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(folderPath);

        cancellationToken.ThrowIfCancellationRequested();

        await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Scanning folder: '{folderPath}'", cancellationToken);

        // For initial implementation, scan the folder tree
        // Note: For large OneDrive accounts (100k+ files), this can take several minutes
        // In future sprints, this will be enhanced with proper delta query support
        var changes = new List<FileMetadata>();
        var rootItem = await GetFolderItemAsync(accountId, folderPath, cancellationToken);

        if (rootItem is not null)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Folder item found, starting recursive scan", cancellationToken);
            // Add a practical limit for initial scan to prevent timeout
            // This will be removed when we implement proper delta query support
            const int maxFiles = 10000; // Limit initial scan to 10k files
            await ScanFolderRecursiveAsync(accountId, rootItem, folderPath, changes, cancellationToken, maxFiles);
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Scan complete: {changes.Count} files found in {folderPath}");
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Scan complete: {changes.Count} files found", cancellationToken);
        }
        else
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.DetectChangesAsync", $"Folder item not found for path: '{folderPath}'", null, cancellationToken);
        }

        // Generate a simple delta token based on timestamp
        // In production, this would be the actual deltaLink from Graph API
        var newDeltaLink = $"delta_{DateTime.UtcNow:yyyyMMddHHmmss}";

        await DebugLog.ExitAsync("RemoteChangeDetector.DetectChangesAsync", cancellationToken);

        return (changes, newDeltaLink);
    }

    private async Task<DriveItem?> GetFolderItemAsync(string accountId, string folderPath, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.GetFolderItemAsync", cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Looking for folder: '{folderPath}'", cancellationToken);

        // For root or empty path, return the drive root
        if (folderPath == "/" || string.IsNullOrEmpty(folderPath))
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", "Returning drive root", cancellationToken);
            return await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        }

        // Clean up the path
        folderPath = folderPath.Trim('/');
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Cleaned path: '{folderPath}'", cancellationToken);

        // Get root and traverse path segments
        var currentItem = await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        if (currentItem?.Id is null)
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.GetFolderItemAsync", "Failed to get drive root", null, cancellationToken);
            return null;
        }

        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Path segments: [{string.Join(", ", pathSegments)}]", cancellationToken);

        foreach (var segment in pathSegments)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Looking for segment: '{segment}'", cancellationToken);
            var children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, currentItem.Id, cancellationToken);
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Found {children.Count()} children in current folder", cancellationToken);

            currentItem = children.FirstOrDefault(c =>
                c.Name?.Equals(segment, StringComparison.OrdinalIgnoreCase) == true &&
                c.Folder is not null);

            if (currentItem?.Id is null)
            {
                await DebugLog.ErrorAsync("RemoteChangeDetector.GetFolderItemAsync", $"Folder segment '{segment}' not found", null, cancellationToken);
                return null;
            }
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Found folder: '{currentItem.Name}' (ID: {currentItem.Id})", cancellationToken);
        }

        await DebugLog.ExitAsync("RemoteChangeDetector.GetFolderItemAsync", cancellationToken);

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
        await DebugLog.EntryAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Scanning folder: '{currentPath}' (ID: {parentItem.Id})", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (changes.Count >= maxFiles)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Max files limit ({maxFiles}) reached", cancellationToken);
            return; // Reached the limit
        }

        if (parentItem.Id is null)
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", "Parent item ID is null", null, cancellationToken);
            return;
        }

        var children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, parentItem.Id, cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Found {children.Count()} items in '{currentPath}'", cancellationToken);

        var fileCount = 0;
        var folderCount = 0;

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
                fileCount++;
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
                folderCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Recursing into subfolder: '{item.Name}'", cancellationToken);
                await ScanFolderRecursiveAsync(accountId, item, itemPath, changes, cancellationToken, maxFiles);
            }
        }

        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Completed '{currentPath}': {fileCount} files, {folderCount} subfolders", cancellationToken);

        await DebugLog.ExitAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", cancellationToken);
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
