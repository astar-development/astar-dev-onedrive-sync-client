using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

public sealed record DeltaPage(IEnumerable<DriveItemEntity> Items, string? NextLink, string? DeltaLink);
