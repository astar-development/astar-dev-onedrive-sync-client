namespace AStar.Dev.OneDrive.Client.Core.Data.Entities;

/// <summary>
///     Entity for storing account information in the database.
/// </summary>
public sealed class AccountEntity
{
    public required string AccountId { get; set; }
    public required string DisplayName { get; set; }
    public required string LocalSyncPath { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
    public string? DeltaToken { get; set; }
    public bool EnableDetailedSyncLogging { get; set; }
    public bool EnableDebugLogging { get; set; }
    public int MaxParallelUpDownloads { get; set; }
    public int MaxItemsInBatch { get; set; }
    public int? AutoSyncIntervalMinutes { get; set; }

    public static AccountEntity CreateSystemAccount()
        => new()
        {
            AccountId = AdminAccountMetadata.AccountId,
            DisplayName = "System Admin",
            LocalSyncPath = ".",
            AutoSyncIntervalMinutes = 0,
            DeltaToken = null,
            EnableDebugLogging = true,
            EnableDetailedSyncLogging = true,
            IsAuthenticated = true,
            LastSyncUtc = null,
            MaxItemsInBatch = 1,
            MaxParallelUpDownloads = 1
        };
}
