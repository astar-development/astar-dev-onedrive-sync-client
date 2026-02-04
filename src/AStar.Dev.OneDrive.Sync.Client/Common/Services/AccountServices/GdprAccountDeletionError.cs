namespace AStar.Dev.OneDrive.Sync.Client.Common.Services.AccountServices;

/// <summary>
/// Represents errors that can occur during GDPR-compliant account deletion.
/// </summary>
public enum GdprAccountDeletionError
{
    /// <summary>
    /// Account with the specified hashed ID was not found.
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// Error occurred while deleting the account from the repository.
    /// </summary>
    RepositoryError,

    /// <summary>
    /// Error occurred while deleting secure token storage.
    /// </summary>
    SecureStorageError,

    /// <summary>
    /// Partial deletion - account deleted but token cleanup failed.
    /// </summary>
    PartialDeletion,

    /// <summary>
    /// Unexpected error occurred during deletion.
    /// </summary>
    UnexpectedError
}
