namespace AStar.Dev.OneDrive.Client;

/// <summary>
///    Provides functionality to hash account IDs for secure logging.
/// </summary>
public static class AccountIdHasher
{
    // Ideally from config/secret, not hard-coded
    private static readonly byte[] Key = "astar-dev-onedrive-client-secret-key"u8.ToArray();

    /// <summary>
    ///   Hashes the given account ID using HMACSHA256.
    /// </summary>
    /// <param name="accountId">The account ID to hash.</param>
    /// <returns>The hashed account ID as a hexadecimal string.</returns>
    public static string Hash(string accountId)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Key);
        var bytes = System.Text.Encoding.UTF8.GetBytes(accountId);
        var hash = hmac.ComputeHash(bytes);

        return Convert.ToHexString(hash);
    }
}
