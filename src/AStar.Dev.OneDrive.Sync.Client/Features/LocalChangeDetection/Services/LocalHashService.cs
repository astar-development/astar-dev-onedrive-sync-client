using System.Security.Cryptography;

namespace AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

/// <summary>
/// Computes and compares local file hashes using SHA256 for change detection.
/// SHA256 provides cryptographic integrity checking and is FIPS-compliant.
/// </summary>
public class LocalHashService : ILocalHashService
{

    /// <summary>
    /// Computes the SHA256 hash of a local file.
    /// Returns null if file does not exist (soft failure for missing files during cleanup).
    /// </summary>
    public async Task<string?> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    /// <summary>
    /// Compares computed file hash with remote hash for change detection.
    /// Returns false if file does not exist or hashes differ.
    /// </summary>
    public async Task<bool> CompareHashesAsync(string filePath, string? remoteHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(remoteHash))
        {
            return false;
        }

        var localHash = await ComputeFileHashAsync(filePath, cancellationToken);

        return string.Equals(localHash, remoteHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if local file has changed by comparing with cached local hash.
    /// Returns false if file does not exist (no changes to upload if file deleted).
    /// </summary>
    public async Task<bool> HasLocalChangesAsync(string filePath, string? cachedLocalHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty or whitespace.", nameof(filePath));
        }

        var currentHash = await ComputeFileHashAsync(filePath, cancellationToken);

        if (currentHash is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(cachedLocalHash))
        {
            return true;
        }

        return !string.Equals(currentHash, cachedLocalHash, StringComparison.OrdinalIgnoreCase);
    }
}
