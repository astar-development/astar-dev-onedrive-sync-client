namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Error types that can occur during account creation.
/// </summary>
public enum AccountCreationError
{
    /// <summary>
    /// Error occurred while retrieving user profile from Graph API.
    /// </summary>
    GraphApiError,

    /// <summary>
    /// An account with this email already exists in the system.
    /// </summary>
    AccountAlreadyExists,

    /// <summary>
    /// Error occurred while storing the authentication token in secure storage.
    /// </summary>
    TokenStorageError,

    /// <summary>
    /// Error occurred while persisting the account record to the database.
    /// </summary>
    RepositoryError,

    /// <summary>
    /// Validation error (invalid or missing input data).
    /// </summary>
    ValidationError,

    /// <summary>
    /// An unexpected error occurred during account creation.
    /// </summary>
    UnexpectedError
}
