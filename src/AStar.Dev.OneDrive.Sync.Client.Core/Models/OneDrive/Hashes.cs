using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class Hashes
{
    public string? quickXorHash { get; set; }
    public string? sha1Hash { get; set; }
    public string? sha256Hash { get; set; }
}
