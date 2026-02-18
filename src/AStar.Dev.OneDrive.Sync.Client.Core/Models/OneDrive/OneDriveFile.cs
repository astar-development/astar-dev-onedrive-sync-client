using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class OneDriveFile
{
    public string? mimeType { get; set; }
    public Hashes? hashes { get; set; }
}
