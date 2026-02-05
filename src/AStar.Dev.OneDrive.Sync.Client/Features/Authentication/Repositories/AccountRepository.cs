using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;

/// <summary>
/// Repository for Account persistence using Entity Framework Core.
/// Implements CRUD and query operations for Account entities.
/// </summary>
public class AccountRepository(OneDriveSyncDbContext context) : IAccountRepository
{
    private readonly OneDriveSyncDbContext _context = context;

    /// <summary>
    /// Creates a new account in the database.
    /// </summary>
    public async Task CreateAsync(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves an account by its ID.
    /// </summary>
    public async Task<Account?> GetByIdAsync(Guid id) => await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>
    /// Retrieves an account by its hashed email address.
    /// </summary>
    public async Task<Account?> GetByHashedEmailAsync(string hashedEmail)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(hashedEmail);
        
        return await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.HashedEmail == hashedEmail);
    }

    /// <summary>
    /// Retrieves an account by its hashed account ID.
    /// </summary>
    public async Task<Account?> GetByHashedAccountIdAsync(string hashedAccountId)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(hashedAccountId);
        
        return await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.HashedAccountId == hashedAccountId);
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    public async Task UpdateAsync(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        
        account.UpdatedAt = DateTime.UtcNow;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an account by ID.
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        Account? account = await _context.Accounts.FindAsync(id);
        if (account != null)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Retrieves all accounts from the database.
    /// </summary>
    public async Task<IEnumerable<Account>> GetAllAsync() => await _context.Accounts
            .AsNoTracking()
            .ToListAsync();

    /// <summary>
    /// Checks if an email hash is unique in the database.
    /// </summary>
    public async Task<bool> IsEmailHashUniqueAsync(string hashedEmail)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(hashedEmail);
        
        var exists = await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.HashedEmail == hashedEmail);
        
        return !exists;
    }

    /// <summary>
    /// Retrieves all accounts with admin privileges.
    /// </summary>
    public async Task<IEnumerable<Account>> GetAdminAccountsAsync() => await _context.Accounts
            .AsNoTracking()
            .Where(a => a.IsAdmin)
            .ToListAsync();

    /// <summary>
    /// Checks if an account exists with the given ID.
    /// </summary>
    public async Task<bool> DoesAccountExistAsync(Guid id) => await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.Id == id);
}
