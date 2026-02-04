namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Represents a user profile retrieved from Microsoft Graph API.
/// Used for account creation with GDPR-compliant hashed identifiers.
/// </summary>
/// <param name=\"Email\">The user's email address (will be hashed before storage).</param>
/// <param name=\"AccountId\">The Microsoft account ID (will be hashed before storage).</param>
public sealed record UserProfile(string Email, string AccountId);
