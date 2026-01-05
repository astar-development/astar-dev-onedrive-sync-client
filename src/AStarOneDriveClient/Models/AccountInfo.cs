namespace AStarOneDriveClient.Models;

/// <summary>
/// Represents account information for a OneDrive account.
/// </summary>
/// <param name="AccountId">Unique identifier for the account.</param>
/// <param name="DisplayName">Display name of the account holder.</param>
/// <param name="LocalSyncPath">Local directory path for synchronization.</param>
/// <param name="IsAuthenticated">Indicates whether the account is currently authenticated.</param>
/// <param name="LastSyncUtc">Timestamp of the last successful synchronization.</param>
/// <param name="DeltaToken">Delta token for incremental synchronization.</param>
public sealed record AccountInfo(
    string AccountId,
    string DisplayName,
    string LocalSyncPath,
    bool IsAuthenticated,
    DateTime? LastSyncUtc,
    string? DeltaToken
);
