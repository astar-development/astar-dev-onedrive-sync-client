using System.Text.Json.Serialization;

namespace AStar.Dev.OneDrive.Client.Core.Models;

public class OneDriveResponse
{
    [JsonPropertyName("@odata.context")]
    public string _odata_context { get; set; }

    [JsonPropertyName("@odata.nextLink")]
    public string _odata_nextLink { get; set; }

    [JsonPropertyName("@odata.deltaLink")]
    public string? _odata_deltaLink { get; set; }

    public Value[] value { get; set; }
}

public class Value
{
    public string createdDateTime { get; set; }
    public string id { get; set; }
    public string lastModifiedDateTime { get; set; }
    public string name { get; set; }
    public string webUrl { get; set; }
    public long size { get; set; }
    public ParentReference parentReference { get; set; }
    public FileSystemInfo fileSystemInfo { get; set; }

    [JsonPropertyName("folder")]
    public OneDriveFolder folder { get; set; }
    public string eTag { get; set; }
    public string cTag { get; set; }
    public SpecialFolder specialFolder { get; set; }

    [JsonPropertyName("file")]
    public OneDriveFile file { get; set; }
    public Image image { get; set; }
}

public class ParentReference
{
    public string driveType { get; set; }
    public string driveId { get; set; }
    public string id { get; set; }
    public string path { get; set; }
    public string siteId { get; set; }
}

public class FileSystemInfo
{
    public string createdDateTime { get; set; }
    public string lastModifiedDateTime { get; set; }
}

public class OneDriveFolder
{
    public int childCount { get; set; }
    public View view { get; set; }
}

public class View
{
    public string sortBy { get; set; }
    public string sortOrder { get; set; }
    public string viewType { get; set; }
}

public class User
{
    public string email { get; set; }
    public string id { get; set; }
    public string displayName { get; set; }
}

public class SpecialFolder
{
    public string name { get; set; }
}

public class OneDriveFile
{
    public string mimeType { get; set; }
    public Hashes hashes { get; set; }
}

public class Hashes
{
    public string quickXorHash { get; set; }
    public string sha1Hash { get; set; }
    public string sha256Hash { get; set; }
}

public class Image
{
    public int height { get; set; }
    public int width { get; set; }
}
