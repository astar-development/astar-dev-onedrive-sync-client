using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

public static class FolderSelectionExtensions
{
    public static void ApplyHierarchicalSelection(this IEnumerable<DriveItemEntity> items, IEnumerable<FileMetadata> fileMetadatas)
        => items.ApplyHierarchicalSelectionCore(fileMetadatas);

    private static void ApplyHierarchicalSelectionCore(this IEnumerable<DriveItemEntity> items, IEnumerable<FileMetadata> fileMetadatas)
    {
        var metaLookup = fileMetadatas
            .ToDictionary(
                    m => Normalize(m.RelativePath),
                    m => m.IsSelected
                );

        foreach(DriveItemEntity item in items)
        {
            var normalized = Normalize(item.RelativePath);
            item.IsSelected = ResolveSelection(normalized, metaLookup);
        }
    }

    private static string Normalize(string path)
    {
        if(string.IsNullOrWhiteSpace(path))
            return "/";

        path = path.Trim()
                   .Replace("\\", "/")
                   .TrimEnd('/');

        return path.StartsWith('/') ? path : "/" + path;
    }

    private static bool? ResolveSelection(string itemPath, IReadOnlyDictionary<string, bool> metaLookup)
    {
        var path = itemPath;

        while(true)
        {
            if(metaLookup.TryGetValue(path, out var selected))
                return selected; // may be true, false, or null

            var lastSlash = path.LastIndexOf('/');
            if(lastSlash <= 0)
                break;
            path = path[..lastSlash];
        }

        return false;

    }
}
