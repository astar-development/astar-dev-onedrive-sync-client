namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;

public record SyncOptions
{
    public const string SectionName = "Sync";

    public int DefaultConcurrentUploads { get; init; } = 5;
    public int DefaultConcurrentDownloads { get; init; } = 5;
    public int DefaultSyncInterval { get; init; } = 300;
    public int ConflictResolutionTimeout { get; init; } = 60;
    public int MaxRetryAttempts { get; init; } = 3;
    public int RetryBackoffSeconds { get; init; } = 5;
}
