using AStar.Dev.OneDrive.Client.Core.Data.Entities;

namespace AStar.Dev.OneDrive.Client.Core.DTOs;

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);
