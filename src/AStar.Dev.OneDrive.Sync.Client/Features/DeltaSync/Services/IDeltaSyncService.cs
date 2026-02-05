namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Services;

/// <summary>
/// Service interface for detecting remote changes using Microsoft Graph API delta queries.
/// </summary>
public interface IDeltaSyncService
{
    /// <summary>
    /// Retrieves changes from OneDrive using delta query.
    /// </summary>
    /// <param name="accessToken">The OAuth access token for authentication.</param>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <param name="driveName">The drive name (e.g., "root").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of detected changes and the new delta token.</returns>
    Task<DeltaSyncResult> GetDeltaChangesAsync(string accessToken, string hashedAccountId, string driveName, CancellationToken cancellationToken = default);
}
