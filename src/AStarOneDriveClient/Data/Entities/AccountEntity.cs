namespace AStarOneDriveClient.Data.Entities;

/// <summary>
/// Entity for storing account information in the database.
/// </summary>
public sealed class AccountEntity
{
    public required string AccountId { get; set; }
    public required string DisplayName { get; set; }
    public required string LocalSyncPath { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTime? LastSyncUtc { get; set; }
    public string? DeltaToken { get; set; }
    public bool EnableDetailedSyncLogging { get; set; }
}
