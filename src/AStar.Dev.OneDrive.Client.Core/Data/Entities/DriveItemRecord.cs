namespace AStar.Dev.OneDrive.Client.Core.Data.Entities;

public record DriveItemRecord(string AccountId, string Id, string DriveItemId, string RelativePath, string? ETag, string? CTag, long Size, DateTimeOffset LastModifiedUtc, bool IsFolder, bool IsDeleted);
