using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Services.OneDriveServices;

/// <summary>
/// Wrapper interface for Microsoft Graph API client to enable testing.
/// </summary>
/// <remarks>
/// Microsoft.Graph.GraphServiceClient and related classes are difficult to mock directly.
/// This interface provides a testable abstraction over OneDrive folder and file operations.
/// </remarks>
public interface IGraphApiClient
{
    /// <summary>
    /// Gets the root drive for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The user's root drive.</returns>
    Task<Drive?> GetMyDriveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the root folder of the user's drive.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The root drive item.</returns>
    Task<DriveItem?> GetDriveRootAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the children (files and folders) of a specific drive item.
    /// </summary>
    /// <param name="itemId">The drive item ID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of child drive items.</returns>
    Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific drive item by its ID.
    /// </summary>
    /// <param name="itemId">The drive item ID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The drive item, or null if not found.</returns>
    Task<DriveItem?> GetDriveItemAsync(string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the children of the root folder.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of root-level drive items.</returns>
    Task<IEnumerable<DriveItem>> GetRootChildrenAsync(CancellationToken cancellationToken = default);
}
