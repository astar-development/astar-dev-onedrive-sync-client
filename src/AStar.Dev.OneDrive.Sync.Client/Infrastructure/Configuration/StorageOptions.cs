namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;

public record StorageOptions
{
    public const string SectionName = "Storage";

    public string DefaultSyncDirectory { get; init; } = "%USERPROFILE%\\OneDriveSync";
    public bool FallbackSecureStorage { get; init; } = true;
}
