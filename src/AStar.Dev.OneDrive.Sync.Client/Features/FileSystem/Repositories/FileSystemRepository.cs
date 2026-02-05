using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Features.FileSystem.Repositories;

/// <summary>
/// Repository for FileSystemItem persistence using Entity Framework Core.
/// Implements CRUD and query operations for FileSystemItem entities.
/// </summary>
public class FileSystemRepository(OneDriveSyncDbContext context) : IFileSystemRepository
{
    private readonly OneDriveSyncDbContext _context = context;

    /// <summary>
    /// Creates a new file system item in the database.
    /// </summary>
    public async Task CreateAsync(FileSystemItem item)
    {
        _ = await _context.FileSystemItems.AddAsync(item);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves a file system item by its ID.
    /// </summary>
    public async Task<FileSystemItem?> GetByIdAsync(string id)
        => await _context.FileSystemItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

    /// <summary>
    /// Retrieves all file system items for a given hashed account ID.
    /// </summary>
    public async Task<IEnumerable<FileSystemItem>> GetAllByHashedAccountIdAsync(string hashedAccountId)
        => await _context.FileSystemItems
            .AsNoTracking()
            .Where(i => i.HashedAccountId == hashedAccountId)
            .ToListAsync();

    /// <summary>
    /// Retrieves all selected file system items for a given hashed account ID.
    /// </summary>
    public async Task<IEnumerable<FileSystemItem>> GetSelectedItemsByHashedAccountIdAsync(string hashedAccountId)
        => await _context.FileSystemItems
            .AsNoTracking()
            .Where(i => i.HashedAccountId == hashedAccountId && i.IsSelected)
            .ToListAsync();

    /// <summary>
    /// Updates an existing file system item in the database.
    /// </summary>
    public async Task UpdateAsync(FileSystemItem item)
    {
        _ = _context.FileSystemItems.Update(item);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a file system item from the database.
    /// </summary>
    public async Task DeleteAsync(string id)
    {
        FileSystemItem? item = await _context.FileSystemItems.FindAsync(id);
        
        if (item is not null)
        {
            _ = _context.FileSystemItems.Remove(item);
            _ = await _context.SaveChangesAsync();
        }
    }
}
