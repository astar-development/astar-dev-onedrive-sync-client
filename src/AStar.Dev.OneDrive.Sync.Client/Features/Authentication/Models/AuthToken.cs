namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Represents a successfully authenticated access token.
/// Refresh tokens are managed internally by MSAL and not exposed.
/// </summary>
public sealed record AuthToken(string AccessToken, DateTime ExpiresAt)
{
    /// <summary>
    /// Gets the number of minutes until token expiry.
    /// </summary>
    public int MinutesUntilExpiry => (int)(ExpiresAt - DateTime.UtcNow).TotalMinutes;

    /// <summary>
    /// Gets a value indicating whether the token should be refreshed proactively (5 minutes before expiry).
    /// </summary>
    public bool ShouldRefreshProactively => MinutesUntilExpiry <= 5;

    /// <summary>
    /// Gets a value indicating whether the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
