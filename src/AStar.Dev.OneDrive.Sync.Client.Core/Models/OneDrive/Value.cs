using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

[ExcludeFromCodeCoverage] // This class is a direct mapping of the OneDrive API response.
public class Value
{
    public string? createdDateTime { get; set; }
    public string? id { get; set; }
    public string? lastModifiedDateTime { get; set; }
    public string? name { get; set; }
    public string? webUrl { get; set; }
    public long size { get; set; }
    public ParentReference? parentReference { get; set; }
    public FileSystemInfo? fileSystemInfo { get; set; }

    [JsonPropertyName("folder")]
    public OneDriveFolder? folder { get; set; }
    public string? eTag { get; set; }
    public string? cTag { get; set; }
    public SpecialFolder? specialFolder { get; set; }

    [JsonPropertyName("file")]
    public OneDriveFile? file { get; set; }
    public Image? image { get; set; }
}
