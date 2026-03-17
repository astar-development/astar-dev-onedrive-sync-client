namespace AStar.Dev.OneDrive.Sync.Client.Models;

public sealed record DriveFolder(
    string  Id,
    string  Name,
    string? ParentId = null);

public sealed record DeltaItem(
    string          Id,
    string          Name,
    string?         ParentId,
    bool            IsFolder,
    bool            IsDeleted,
    long            Size,
    DateTimeOffset? LastModified,
    string?         DownloadUrl);

public sealed record DeltaResult(
    List<DeltaItem> Items,
    string?         NextDeltaLink,
    bool            HasMorePages);
