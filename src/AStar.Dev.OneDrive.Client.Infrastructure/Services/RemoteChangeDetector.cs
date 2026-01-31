using System.Diagnostics;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Service for detecting changes on OneDrive using delta queries.
/// </summary>
/// <remarks>
///     Note: Full delta query support requires Microsoft.Graph SDK capabilities.
///     This implementation provides a foundation that can be extended when delta APIs are integrated.
///     For now, it performs full scans and compares against known state.
/// </remarks>
public sealed class RemoteChangeDetector(IGraphApiClient graphApiClient) : IRemoteChangeDetector
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)>
        DetectChangesAsync(string accountId, string folderPath, string? previousDeltaLink, CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.DetectChangesAsync", accountId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Scanning folder: '{folderPath}'", accountId, cancellationToken);
        var cleanedFolderPath = CleanGraphApiPathPrefix(folderPath);
        if(cleanedFolderPath != folderPath)
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Cleaned folder path from '{folderPath}' to '{cleanedFolderPath}'", accountId, cancellationToken);

        // For initial implementation, scan the folder tree
        // Note: For large OneDrive accounts (100k+ files), this can take several minutes
        // In future sprints, this will be enhanced with proper delta query support
        var changes = new List<FileMetadata>();
        DriveItem? rootItem = await GetFolderItemAsync(accountId, folderPath, cancellationToken);

        if(rootItem is not null)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", "Folder item found, starting recursive scan", accountId, cancellationToken);
            // Add a practical limit for initial scan to prevent timeout
            // This will be removed when we implement proper delta query support
            const int maxFiles = 10000; // Limit initial scan to 10k files
            // Use cleaned path for building file paths
            await ScanFolderRecursiveAsync(accountId, rootItem, cleanedFolderPath, changes, cancellationToken, maxFiles);
            Debug.WriteLine($"[RemoteChangeDetector] Scan complete: {changes.Count} files found in {cleanedFolderPath}");
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Scan complete: {changes.Count} files found", accountId, cancellationToken);
        }
        else
        {
            var errorMessage = $"Remote folder not found: '{folderPath}'. Please verify the folder exists and you have access to it.";
            await DebugLog.ErrorAsync("RemoteChangeDetector.DetectChangesAsync", errorMessage, accountId, null, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        // Generate a simple delta token based on timestamp
        // In production, this would be the actual deltaLink from Graph API
        var newDeltaLink = $"delta_{DateTime.UtcNow:yyyyMMddHHmmss}";

        await DebugLog.ExitAsync("RemoteChangeDetector.DetectChangesAsync", accountId, cancellationToken);

        return (changes, newDeltaLink);
    }

    private async Task<DriveItem?> GetFolderItemAsync(string accountId, string folderPath, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.GetFolderItemAsync", accountId, cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Looking for folder: '{folderPath}'", accountId, cancellationToken);

        // Defensive: Strip Graph API path prefixes that might be in stored paths
        // This handles cases where paths like "/drive/root:/Documents" or "/drives/{id}/root:/Documents" were stored
        if(folderPath.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
        {
            folderPath = folderPath["/drive/root:".Length..];
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Stripped '/drive/root:' prefix, new path: '{folderPath}'", accountId, cancellationToken);
        }
        else if(folderPath.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = folderPath.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if(rootIndex >= 0)
            {
                folderPath = folderPath[(rootIndex + "/root:".Length)..];
                await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Stripped '/drives/.../root:' prefix, new path: '{folderPath}'", accountId, cancellationToken);
            }
        }

        // For root or empty path, return the drive root
        if(folderPath == "/" || string.IsNullOrEmpty(folderPath))
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", "Returning drive root", accountId, cancellationToken);
            return await graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        }

        // Clean up the path
        folderPath = folderPath.Trim('/');
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Trimmed path: '{folderPath}'", accountId, cancellationToken);

        // Get root and traverse path segments
        DriveItem? currentItem = await graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        if(currentItem?.Id is null)
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.GetFolderItemAsync", "Failed to get drive root", accountId, null, cancellationToken);
            return null;
        }

        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Path segments: [{string.Join(", ", pathSegments)}]", accountId, cancellationToken);

        foreach(var segment in pathSegments)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Looking for segment: '{segment}'", accountId, cancellationToken);
            IEnumerable<DriveItem> children = await graphApiClient.GetDriveItemChildrenAsync(accountId, currentItem.Id, cancellationToken);
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Found {children.Count()} children in current folder", accountId, cancellationToken);

            currentItem = children.FirstOrDefault(c => c.Name?.Equals(segment, StringComparison.OrdinalIgnoreCase) == true &&
                                                       c.Folder is not null);

            if(currentItem?.Id is null)
            {
                await DebugLog.ErrorAsync("RemoteChangeDetector.GetFolderItemAsync", $"Folder segment '{segment}' not found", accountId, null, cancellationToken);
                return null;
            }

            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Found folder: '{currentItem.Name}' (ID: {currentItem.Id})", accountId, cancellationToken);
        }

        await DebugLog.ExitAsync("RemoteChangeDetector.GetFolderItemAsync", accountId, cancellationToken);

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
        await DebugLog.EntryAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", accountId, cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Scanning folder: '{currentPath}' (ID: {parentItem.Id})", accountId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if(changes.Count >= maxFiles)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Max files limit ({maxFiles}) reached", accountId, cancellationToken);
            return; // Reached the limit
        }

        if(parentItem.Id is null)
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", "Parent item ID is null", accountId, null, cancellationToken);
            return;
        }

        IEnumerable<DriveItem> children = await graphApiClient.GetDriveItemChildrenAsync(accountId, parentItem.Id, cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Found {children.Count()} items in '{currentPath}'", accountId, cancellationToken);

        var fileCount = 0;
        var folderCount = 0;

        foreach(DriveItem item in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(changes.Count >= maxFiles)
                return; // Reached the limit

            if(item.File is not null && item.Id is not null && item.Name is not null)
            {
                // It's a file
                fileCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                FileMetadata metadata = ConvertToFileMetadata(accountId, item, itemPath);
                changes.Add(metadata);
                if(changes.Count % 500 == 0)
                    Debug.WriteLine($"[RemoteChangeDetector] Progress: {changes.Count} files scanned");
            }
            else if(item.Folder is not null && item.Id is not null && item.Name is not null)
            {
                // It's a folder - scan recursively
                folderCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Recursing into subfolder: '{item.Name}'", accountId, cancellationToken);
                await ScanFolderRecursiveAsync(accountId, item, itemPath, changes, cancellationToken, maxFiles);
            }
        }

        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Completed '{currentPath}': {fileCount} files, {folderCount} subfolders", accountId, cancellationToken);

        await DebugLog.ExitAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", accountId, cancellationToken);
    }

    private static FileMetadata ConvertToFileMetadata(string accountId, DriveItem item, string path) => new(
        item.Id ?? string.Empty,
        accountId,
        item.Name ?? string.Empty,
        path,
        item.Size ?? 0,
        item.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
        string.Empty, // Will be set during download
        false,
        false,
        false,
        item.CTag,
        item.ETag,
        null, // Will be computed after download
        null,
        FileSyncStatus.PendingDownload,
        SyncDirection.Download);

    private static string CombinePaths(string basePath, string name)
    {
        basePath = basePath.Replace('\\', '/');
        name = name.Replace('\\', '/');

        if(!basePath.EndsWith('/'))
            basePath += '/';

        if(name.StartsWith('/'))
            name = name[1..];

        return basePath + name;
    }

    /// <summary>
    ///     Cleans Graph API path prefixes from folder paths.
    /// </summary>
    /// <param name="path">The path that may contain Graph API prefixes.</param>
    /// <returns>The cleaned path without Graph API prefixes.</returns>
    private static string CleanGraphApiPathPrefix(string path)
    {
        if(string.IsNullOrEmpty(path))
            return path;

        // Strip /drive/root: prefix
        if(path.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
            return path["/drive/root:".Length..];

        // Strip /drives/{drive-id}/root: prefix
        if(path.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = path.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if(rootIndex >= 0)
                return path[(rootIndex + "/root:".Length)..];
        }

        return path;
    }
}
