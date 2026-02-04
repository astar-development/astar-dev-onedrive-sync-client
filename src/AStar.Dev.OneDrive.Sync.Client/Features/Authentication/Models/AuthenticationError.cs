namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Represents different authentication error scenarios using discriminated unions.
/// </summary>
public abstract record AuthenticationError
{
    /// <summary>
    /// User cancelled the authentication process.
    /// </summary>
    public sealed record Cancelled : AuthenticationError;

    /// <summary>
    /// Authentication operation timed out.
    /// </summary>
    public sealed record TimedOut : AuthenticationError;

    /// <summary>
    /// MSAL client configuration error (missing credentials, invalid tenant, etc.).
    /// </summary>
    public sealed record ConfigurationError(string Message) : AuthenticationError;

    /// <summary>
    /// Network or HTTP error during authentication.
    /// </summary>
    public sealed record NetworkError(string Message) : AuthenticationError;

    /// <summary>
    /// Azure AD / Microsoft authentication service returned an error.
    /// </summary>
    public sealed record ServiceError(string Code, string Message) : AuthenticationError;

    /// <summary>
    /// Unexpected error during authentication.
    /// </summary>
    public sealed record UnexpectedError(string Message) : AuthenticationError;
}
