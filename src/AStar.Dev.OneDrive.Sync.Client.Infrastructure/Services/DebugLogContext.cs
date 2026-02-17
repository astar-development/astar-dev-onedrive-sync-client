using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Provides ambient context for the current account being processed.
///     Uses AsyncLocal to flow context through async operations without explicit parameter passing.
/// </summary>
public static class DebugLogContext
{
    private static readonly AsyncLocal<string?> _currentAccountId = new();

    /// <summary>
    ///     Gets the current account ID from the ambient context.
    /// </summary>
    public static string CurrentAccountId => _currentAccountId.Value ?? AdminAccountMetadata.HashedAccountId;

    /// <summary>
    ///     Sets the current account ID for the ambient context.
    ///     This flows through all async operations in the current execution context.
    /// </summary>
    /// <param name="accountId">The account ID to set.</param>
    public static void SetAccountId(HashedAccountId? accountId) => _currentAccountId.Value = accountId?.Value;

    /// <summary>
    ///     Clears the current account ID from the ambient context.
    /// </summary>
    public static void Clear() => _currentAccountId.Value = null;
}
