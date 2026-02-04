using System.Security.Cryptography;
using System.Text;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Implements SHA256 hashing for email and account ID obfuscation.
/// Ensures consistent, deterministic hashes for database lookups.
/// </summary>
public class HashingService : IHashingService
{
    /// <inheritdoc/>
    public async Task<string> HashEmailAsync(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return string.IsNullOrWhiteSpace(email)
            ? throw new ArgumentException("Email cannot be empty or whitespace.", nameof(email))
            : await Task.FromResult(HashValue(email.ToLowerInvariant()));
    }

    /// <inheritdoc/>
    public async Task<string> HashAccountIdAsync(string accountId, long createdAtTicks)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        if(string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be empty or whitespace.", nameof(accountId));
        }

        var saltedValue = $"{accountId}:{createdAtTicks}";

        return await Task.FromResult(HashValue(saltedValue));
    }

    /// <summary>
    /// Hashes a string value using SHA256 and returns the hex-encoded result.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>A 64-character hex-encoded SHA256 hash.</returns>
    private static string HashValue(string value)
    {
        var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }
}
