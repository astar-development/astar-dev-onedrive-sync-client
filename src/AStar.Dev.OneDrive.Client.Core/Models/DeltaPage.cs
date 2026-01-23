using AStar.Dev.OneDrive.Client.Core.Data.Entities;

namespace AStar.Dev.OneDrive.Client.Core.Models;

public sealed record DeltaPage(IEnumerable<DriveItemEntity> Items, string? NextLink, string? DeltaLink);
