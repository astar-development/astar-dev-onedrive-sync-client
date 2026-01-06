using AStarOneDriveClient.Models;

namespace AStarOneDriveClient.Services;

/// <summary>
/// Service for scanning local file system and detecting file changes.
/// </summary>
public interface ILocalFileScanner
{
    /// <summary>
    /// Scans a local folder and returns metadata for all files found.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="localFolderPath">The local folder path to scan.</param>
    /// <param name="oneDriveFolderPath">The corresponding OneDrive folder path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file metadata for all files in the folder and subfolders.</returns>
    Task<IReadOnlyList<FileMetadata>> ScanFolderAsync(
        string accountId,
        string localFolderPath,
        string oneDriveFolderPath,
        CancellationToken cancellationToken = default);
}
