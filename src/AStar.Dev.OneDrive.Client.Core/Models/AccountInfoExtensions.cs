namespace AStar.Dev.OneDrive.Client.Core.Models;

/// <summary>
///   Extension methods for creating account information.
/// </summary>
public static class AccountInfoExtensions
{
    extension(AccountInfo result)
    {
        /// <summary>
        ///   Creates a standard <see cref="AccountInfo"/> instance with default settings for a new account.
        /// </summary>
        /// <param name="hashedAccountId">
        ///  The hashed unique identifier for the account.
        /// </param>
        /// <param name="displayName">
        ///  The display name of the account holder.
        /// </param>
        /// <param name="localSyncPath">
        ///  The local directory path for synchronization.
        /// </param>
        /// <returns>A standard <see cref="AccountInfo"/> instance with default settings.</returns>
        public static AccountInfo Standard(string hashedAccountId, string displayName, string localSyncPath) => new(hashedAccountId, displayName, localSyncPath, true, null, null, false, false, 3, 20, null);
    }
}
