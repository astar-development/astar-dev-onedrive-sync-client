using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class ParentReference
{
    public string? driveType { get; set; }
    public string? driveId { get; set; }
    public string? id { get; set; }
    public string? path { get; set; }
    public string? siteId { get; set; }
}
