using System.Diagnostics;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

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
    public async Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)> DetectChangesAsync(string accountId, HashedAccountId hashedAccountId, string folderPath, string? previousDeltaLink, CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, $"Scanning folder: '{folderPath}'", cancellationToken);
        var cleanedFolderPath = CleanGraphApiPathPrefix(folderPath);
        if(cleanedFolderPath != folderPath)
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, $"Cleaned folder path from '{folderPath}' to '{cleanedFolderPath}'", cancellationToken);

        var changes = new List<FileMetadata>();
        DriveItem? rootItem = await GetFolderItemAsync(accountId, hashedAccountId, folderPath, cancellationToken);

        if(rootItem is not null)
        {
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, "Folder item found, starting recursive scan", cancellationToken);
            const int maxFiles = 1_000_000;
            await ScanFolderRecursiveAsync(accountId, hashedAccountId, rootItem, cleanedFolderPath, changes, cancellationToken, maxFiles);
            Debug.WriteLine($"[RemoteChangeDetector] Scan complete: {changes.Count} files found in {cleanedFolderPath}");
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, $"Scan complete: {changes.Count} files found", cancellationToken);
        }
        else
        {
            var errorMessage = $"Remote folder not found: '{folderPath}'. Please verify the folder exists and you have access to it.";
            _ = await DebugLog.LogErrorAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, errorMessage, null, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        var newDeltaLink = $"delta_{DateTime.UtcNow:yyyyMMddHHmmss}";

        await DebugLog.ExitAsync("RemoteChangeDetector.DetectChangesAsync", hashedAccountId, cancellationToken);

        return (changes, newDeltaLink);
    }

    private async Task<DriveItem?> GetFolderItemAsync(string accountId, HashedAccountId hashedAccountId, string folderPath, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, cancellationToken);
        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Looking for folder: '{folderPath}'", cancellationToken);

        if(folderPath.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
        {
            folderPath = folderPath["/drive/root:".Length..];
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Stripped '/drive/root:' prefix, new path: '{folderPath}'", cancellationToken);
        }
        else if(folderPath.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = folderPath.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if(rootIndex >= 0)
            {
                folderPath = folderPath[(rootIndex + "/root:".Length)..];
                _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Stripped '/drives/.../root:' prefix, new path: '{folderPath}'", cancellationToken);
            }
        }

        if(folderPath == "/" || string.IsNullOrEmpty(folderPath))
        {
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, "Returning drive root", cancellationToken);
            return await graphApiClient.GetDriveRootAsync(accountId, hashedAccountId, cancellationToken);
        }

        folderPath = folderPath.Trim('/');
        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Trimmed path: '{folderPath}'", cancellationToken);

        DriveItem? currentItem = await graphApiClient.GetDriveRootAsync(accountId, hashedAccountId, cancellationToken);
        if(currentItem?.Id is null)
        {
            _ = await DebugLog.LogErrorAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, "Failed to get drive root", null, cancellationToken);
            return null;
        }

        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Path segments: [{string.Join(", ", pathSegments)}]", cancellationToken);

        foreach(var segment in pathSegments)
        {
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Looking for segment: '{segment}'", cancellationToken);
            IEnumerable<DriveItem> children = await graphApiClient.GetDriveItemChildrenAsync(accountId, hashedAccountId, currentItem.Id, cancellationToken);
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Found {children.Count()} children in current folder", cancellationToken);

            currentItem = children.FirstOrDefault(c => c.Name?.Equals(segment, StringComparison.OrdinalIgnoreCase) == true && c.Folder is not null);

            if(currentItem?.Id is null)
            {
                _ = await DebugLog.LogErrorAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Folder segment '{segment}' not found", null, cancellationToken);
                return null;
            }

            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, $"Found folder: '{currentItem.Name}' (ID: {currentItem.Id})", cancellationToken);
        }

        await DebugLog.ExitAsync("RemoteChangeDetector.GetFolderItemAsync", hashedAccountId, cancellationToken);

        return currentItem;
    }

    private async Task ScanFolderRecursiveAsync(string accountId, HashedAccountId hashedAccountId, DriveItem parentItem, string currentPath, List<FileMetadata> changes, CancellationToken cancellationToken, int maxFiles = int.MaxValue)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, cancellationToken);
        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, $"Scanning folder: '{currentPath}' (ID: {parentItem.Id})", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if(changes.Count >= maxFiles)
        {
            _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, $"Max files limit ({maxFiles}) reached", cancellationToken);
            return;
        }

        if(parentItem.Id is null)
        {
            _ = await DebugLog.LogErrorAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, "Parent item ID is null", null, cancellationToken);
            return;
        }

        IEnumerable<DriveItem> children = await graphApiClient.GetDriveItemChildrenAsync(accountId, hashedAccountId, parentItem.Id, cancellationToken);
        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, $"Found {children.Count()} items in '{currentPath}'", cancellationToken);

        var fileCount = 0;
        var folderCount = 0;

        foreach(DriveItem item in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(changes.Count >= maxFiles)
                return;

            if(item.File is not null && item.Id is not null && item.Name is not null)
            {
                fileCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                FileMetadata metadata = ConvertToFileMetadata(hashedAccountId, item, itemPath);
                changes.Add(metadata);
                if(changes.Count % 500 == 0)
                    Debug.WriteLine($"[RemoteChangeDetector] Progress: {changes.Count} files scanned");
            }
            else if(item.Folder is not null && item.Id is not null && item.Name is not null)
            {
                folderCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, $"Recursing into subfolder: '{item.Name}'", cancellationToken);
                await ScanFolderRecursiveAsync(accountId, hashedAccountId, item, itemPath, changes, cancellationToken, maxFiles);
            }
        }

        _ = await DebugLog.LogInfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, $"Completed '{currentPath}': {fileCount} files, {folderCount} subfolders", cancellationToken);

        await DebugLog.ExitAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", hashedAccountId, cancellationToken);
    }

    private static FileMetadata ConvertToFileMetadata(HashedAccountId hashedAccountId, DriveItem item, string path) => new(item.Id ?? string.Empty, hashedAccountId, item.Name ?? string.Empty, path, item.Size ?? 0, item.LastModifiedDateTime ?? DateTimeOffset.UtcNow, string.Empty, false, false, false, item.CTag, item.ETag, null, null, FileSyncStatus.PendingDownload, SyncDirection.Download);

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

    private static string CleanGraphApiPathPrefix(string path)
    {
        if(string.IsNullOrEmpty(path))
            return path;

        if(path.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
            return path["/drive/root:".Length..];

        if(path.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = path.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if(rootIndex >= 0)
                return path[(rootIndex + "/root:".Length)..];
        }

        return path;
    }
}
