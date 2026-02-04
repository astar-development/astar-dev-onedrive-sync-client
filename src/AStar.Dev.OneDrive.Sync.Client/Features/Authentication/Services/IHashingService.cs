namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Service for hashing sensitive data using SHA256.
/// Used for email and account ID obfuscation in the database.
/// </summary>
public interface IHashingService
{
    /// <summary>
    /// Hashes an email address using SHA256.
    /// Email hashing is case-insensitive (emails are normalized to lowercase before hashing).
    /// </summary>
    /// <param name="email">The email address to hash.</param>
    /// <returns>A hex-encoded SHA256 hash of the lowercase email.</returns>
    Task<string> HashEmailAsync(string email);

    /// <summary>
    /// Hashes an account ID using SHA256 with a salt.
    /// The salt is derived from createdAtTicks to ensure unique hashes per account.
    /// </summary>
    /// <param name="accountId">The account ID to hash (e.g., from Microsoft Graph API).</param>
    /// <param name="createdAtTicks">The creation timestamp in ticks, used as salt.</param>
    /// <returns>A hex-encoded SHA256 hash of the account ID combined with the salt.</returns>
    Task<string> HashAccountIdAsync(string accountId, long createdAtTicks);
}
