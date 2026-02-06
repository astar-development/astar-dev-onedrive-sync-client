namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

/// <summary>
/// Service for computing and comparing local file hashes for change detection.
/// Uses SHA256 cryptographic hashing for integrity verification.
/// </summary>
public interface ILocalHashService
{
    /// <summary>
    /// Computes the hash of a local file.
    /// </summary>
    /// <param name="filePath">The full path to the file to hash.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The hex-encoded hash of the file, or null if file not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when filePath is empty or whitespace.</exception>
    /// <exception cref="System.IO.IOException">Thrown when file cannot be read.</exception>
    Task<string?> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares a local file hash with a remote hash for change detection.
    /// </summary>
    /// <param name="filePath">The full path to the local file.</param>
    /// <param name="remoteHash">The hash from remote (OneDrive).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if hashes match (no changes), false if different or file not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when filePath is empty or whitespace.</exception>
    Task<bool> CompareHashesAsync(string filePath, string? remoteHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a local file has changed since last sync by comparing with cached local hash.
    /// </summary>
    /// <param name="filePath">The full path to the local file.</param>
    /// <param name="cachedLocalHash">The cached hash from last sync.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if file has changed, false if unchanged or file not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when filePath is empty or whitespace.</exception>
    Task<bool> HasLocalChangesAsync(string filePath, string? cachedLocalHash, CancellationToken cancellationToken = default);
}
