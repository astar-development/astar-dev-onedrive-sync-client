using System.Text.Json.Serialization;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models.OneDrive;

public class OneDriveResponse
{
    [JsonPropertyName("@odata.context")]
    public string? OdataContext { get; set; }

    [JsonPropertyName("@odata.nextLink")]
    public string? OdataNextLink { get; set; }

    [JsonPropertyName("@odata.deltaLink")]
    public string? OdataDeltaLink { get; set; }

    public Value[]? Value { get; set; }
}
