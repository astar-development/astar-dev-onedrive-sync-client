using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

/// <summary>
///     Entity for storing account information in the database.
/// </summary>
public sealed class AccountEntity
{
    public required string Id { get; set; }
    public required HashedAccountId HashedAccountId { get; set; }
    public required string DisplayName { get; set; }
    public required string LocalSyncPath { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
    public string? DeltaToken { get; set; }
    public bool EnableDetailedSyncLogging { get; set; }
    public bool EnableDebugLogging { get; set; }
    public int MaxParallelUpDownloads { get; set; }
    public int MaxItemsInBatch { get; set; }
    public int AutoSyncIntervalMinutes { get; set; }

    public static AccountEntity CreateSystemAccount() => new()
    {
        Id = "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9",
        HashedAccountId = new HashedAccountId(AdminAccountMetadata.HashedAccountId),
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
