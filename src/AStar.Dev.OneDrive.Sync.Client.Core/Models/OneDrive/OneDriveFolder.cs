using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class OneDriveFolder
{
    public int childCount { get; set; }
    public View? view { get; set; }
}
