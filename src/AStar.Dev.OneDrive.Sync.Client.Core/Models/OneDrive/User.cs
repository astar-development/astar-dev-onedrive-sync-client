using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class User
{
    public string? email { get; set; }
    public string? id { get; set; }
    public string? displayName { get; set; }
}
