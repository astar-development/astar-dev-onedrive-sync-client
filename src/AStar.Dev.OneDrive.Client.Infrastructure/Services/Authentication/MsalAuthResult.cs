using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;

/// <summary>
///     Simplified authentication result wrapper to avoid dependencies on MSAL's sealed types in tests.
/// </summary>
public sealed class MsalAuthResult(IAccount account, string accessToken)
{
    /// <summary>
    ///     Gets the authenticated account.
    /// </summary>
    public IAccount Account { get; init; } = account;

    /// <summary>
    ///     Gets the access token.
    /// </summary>
    public string AccessToken { get; init; } = accessToken;

    /// <summary>
    ///     Creates a wrapper from MSAL's AuthenticationResult.
    /// </summary>
    /// <param name="msalResult">The MSAL authentication result.</param>
    /// <returns>Wrapped result.</returns>
    public static MsalAuthResult FromMsal(Microsoft.Identity.Client.AuthenticationResult msalResult)
        => new(msalResult.Account, msalResult.AccessToken);
}
