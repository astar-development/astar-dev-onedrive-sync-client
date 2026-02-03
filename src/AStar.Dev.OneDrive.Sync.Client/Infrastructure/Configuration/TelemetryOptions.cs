namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;

public record TelemetryOptions
{
    public const string SectionName = "Telemetry";
    
    public bool Enabled { get; init; } = true;
    public bool ExportToDatabase { get; init; } = true;
    public int LogRetentionDays { get; init; } = 15;
    public int CriticalLogRetentionDays { get; init; } = 30;
}
