using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;

/// <summary>
///     Result of an authentication operation.
/// </summary>
/// <param name="Success">Indicates whether the operation was successful.</param>
/// <param name="HashedAccountId">The hashed identifier of the authenticated account.</param>
/// <param name="DisplayName">The display name of the authenticated user.</param>
/// <param name="ErrorMessage">Error message if the operation failed.</param>
public sealed record AuthenticationResult(bool Success, string AccountId,HashedAccountId HashedAccountId, string DisplayName, string? ErrorMessage = null);
