using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;

/// <summary>
/// Repository interface for Account persistence operations.
/// Provides CRUD and query methods for Account entities.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Creates a new account in the database.
    /// </summary>
    /// <param name="account">The account to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(Account account);

    /// <summary>
    /// Retrieves an account by its ID.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <returns>The account if found; otherwise null.</returns>
    Task<Account?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves an account by its hashed email address.
    /// </summary>
    /// <param name="hashedEmail">The hashed email to search for.</param>
    /// <returns>The account if found; otherwise null.</returns>
    Task<Account?> GetByHashedEmailAsync(string hashedEmail);

    /// <summary>
    /// Retrieves an account by its hashed account ID.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account ID to search for.</param>
    /// <returns>The account if found; otherwise null.</returns>
    Task<Account?> GetByHashedAccountIdAsync(string hashedAccountId);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="account">The account to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Account account);

    /// <summary>
    /// Deletes an account by ID.
    /// </summary>
    /// <param name="id">The ID of the account to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves all accounts from the database.
    /// </summary>
    /// <returns>An enumerable of all accounts.</returns>
    Task<IEnumerable<Account>> GetAllAsync();

    /// <summary>
    /// Checks if an email hash is unique in the database.
    /// </summary>
    /// <param name="hashedEmail">The hashed email to check.</param>
    /// <returns>True if the email hash is unique; otherwise false.</returns>
    Task<bool> IsEmailHashUniqueAsync(string hashedEmail);

    /// <summary>
    /// Retrieves all accounts with admin privileges.
    /// </summary>
    /// <returns>An enumerable of all admin accounts.</returns>
    Task<IEnumerable<Account>> GetAdminAccountsAsync();

    /// <summary>
    /// Checks if an account exists with the given ID.
    /// </summary>
    /// <param name="id">The account ID to check.</param>
    /// <returns>True if the account exists; otherwise false.</returns>
    Task<bool> DoesAccountExistAsync(Guid id);
}
