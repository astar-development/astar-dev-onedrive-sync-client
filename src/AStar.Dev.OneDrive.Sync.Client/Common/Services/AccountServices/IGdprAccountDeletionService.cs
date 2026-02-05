using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Common.Services.AccountServices;

/// <summary>
/// Service interface for GDPR-compliant account deletion with cascade delete and secure storage cleanup.
/// </summary>
public interface IGdprAccountDeletionService
{
    /// <summary>
    /// Deletes an account and all related data in a GDPR-compliant manner.
    /// CASCADE DELETE handles related entity deletion automatically.
    /// Continues on errors to provide complete deletion status.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <returns>
    /// Result containing true if deletion succeeded completely,
    /// or an error indicating what failed.
    /// </returns>
    Task<Result<bool, GdprAccountDeletionError>> DeleteAccountWithGdprComplianceAsync(string hashedAccountId);
}
