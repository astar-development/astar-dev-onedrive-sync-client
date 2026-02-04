namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Error types that can occur during account management operations.
/// </summary>
public enum AccountManagementError
{
    /// <summary>
    /// The specified account was not found in the database.
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// Error occurred while updating the account in the database.
    /// </summary>
    RepositoryError,

    /// <summary>
    /// Validation error (invalid or missing input data).
    /// </summary>
    ValidationError,

    /// <summary>
    /// An unexpected error occurred during account management.
    /// </summary>
    UnexpectedError
}
