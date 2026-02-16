using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for scanning local file system and detecting file changes.
/// </summary>
public interface ILocalFileScanner
{
    /// <summary>
    ///     Scans a local folder and returns metadata for all files found.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <param name="localFolderPath">The local folder path to scan.</param>
    /// <param name="oneDriveFolderPath">The corresponding OneDrive folder path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file metadata for all files in the folder and subfolders.</returns>
    Task<IReadOnlyList<FileMetadata>> ScanFolderAsync(
        HashedAccountId hashedAccountId,
        string localFolderPath,
        string oneDriveFolderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Computes the SHA256 hash of a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Hexadecimal string representation of the file's SHA256 hash.</returns>
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);
}
