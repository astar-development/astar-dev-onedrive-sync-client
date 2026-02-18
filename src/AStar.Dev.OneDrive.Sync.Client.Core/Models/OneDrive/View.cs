using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class View
{
    public string? sortBy { get; set; }
    public string? sortOrder { get; set; }
    public string? viewType { get; set; }
}
