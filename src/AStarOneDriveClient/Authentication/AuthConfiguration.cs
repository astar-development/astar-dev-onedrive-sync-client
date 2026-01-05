namespace AStarOneDriveClient.Authentication;

/// <summary>
/// Configuration settings for MSAL authentication.
/// </summary>
public static class AuthConfiguration
{
    /// <summary>
    /// Gets the Azure AD client ID for the application.
    /// </summary>
#pragma warning disable S1075 // URIs should not be hardcoded - Required for MSAL OAuth configuration
    public const string ClientId = "YOUR_CLIENT_ID_HERE"; // TODO: Replace with actual client ID

    /// <summary>
    /// Gets the redirect URI for OAuth callbacks.
    /// </summary>
    public const string RedirectUri = "http://localhost";
#pragma warning restore S1075

    /// <summary>
    /// Gets the Microsoft Graph API scopes required for OneDrive access.
    /// </summary>
    public static readonly string[] Scopes = ["Files.ReadWrite", "User.Read", "offline_access"];

    /// <summary>
    /// Gets the authority URL for Microsoft identity platform.
    /// </summary>
    public const string Authority = "https://login.microsoftonline.com/common";
}
