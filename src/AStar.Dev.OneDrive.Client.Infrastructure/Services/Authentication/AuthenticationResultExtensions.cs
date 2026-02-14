namespace AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;

/// <summary>
///    Extension methods for creating standardized authentication results.
/// </summary>
public static class AuthenticationResultExtensions
{
    extension(AuthenticationResult result)
    {
        /// <summary>
        ///     Creates a successful authentication result for the specified account.
        /// </summary>
        /// <param name="hashedAccountId">The hashed identifier of the authenticated account.</param>
        /// <param name="displayName">The display name of the authenticated user.</param>
        /// <returns>An <see cref="AuthenticationResult"/> representing a successful authentication.</returns>
        public static AuthenticationResult Success(string hashedAccountId, string displayName) => new(true, hashedAccountId, displayName);

        /// <summary>
        ///     Creates a failed authentication result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <returns>An <see cref="AuthenticationResult"/> representing a failed authentication.</returns>
        public static AuthenticationResult Failed(string errorMessage) => new(false, string.Empty, string.Empty, errorMessage);
    }
}
