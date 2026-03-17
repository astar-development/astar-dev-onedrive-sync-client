using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Graph;

public interface IGraphService
{
    /// <summary>
    /// Returns the drive ID for the authenticated user.
    /// Result is cached — safe to call repeatedly.
    /// </summary>
    Task<string> GetDriveIdAsync(
        string accessToken,
        CancellationToken ct = default);

    /// <summary>Returns the root-level folders in the user's OneDrive.</summary>
    Task<List<DriveFolder>> GetRootFoldersAsync(
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the immediate child folders of the given parent folder.
    /// Used for lazy-loading the folder tree.
    /// </summary>
    Task<List<DriveFolder>> GetChildFoldersAsync(
        string accessToken,
        string driveId,
        string parentFolderId,
        CancellationToken ct = default);

    /// <summary>Returns the user's OneDrive storage quota.</summary>
    Task<(long Total, long Used)> GetQuotaAsync(
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a delta query for the given folder.
    /// Pass null deltaLink for a full sync (first run).
    /// </summary>
    Task<DeltaResult> GetDeltaAsync(
        string  accessToken,
        string  folderId,
        string? deltaLink,
        CancellationToken ct = default);
}
