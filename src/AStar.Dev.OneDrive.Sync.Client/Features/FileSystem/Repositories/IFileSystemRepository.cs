using AStar.Dev.OneDrive.Sync.Client.Common.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.FileSystem.Repositories;

/// <summary>
/// Repository interface for FileSystemItem persistence operations.
/// Provides CRUD and query methods for FileSystemItem entities.
/// </summary>
public interface IFileSystemRepository
{
    /// <summary>
    /// Creates a new file system item in the database.
    /// </summary>
    /// <param name="item">The file system item to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(FileSystemItem item);

    /// <summary>
    /// Retrieves a file system item by its ID.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>The file system item if found; otherwise null.</returns>
    Task<FileSystemItem?> GetByIdAsync(string id);

    /// <summary>
    /// Retrieves all file system items for a given hashed account ID.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <returns>An enumerable of all file system items for the account.</returns>
    Task<IEnumerable<FileSystemItem>> GetAllByHashedAccountIdAsync(string hashedAccountId);

    /// <summary>
    /// Retrieves all selected file system items for a given hashed account ID.
    /// </summary>
    /// <param name="hashedAccountId">The hashed account identifier.</param>
    /// <returns>An enumerable of selected file system items.</returns>
    Task<IEnumerable<FileSystemItem>> GetSelectedItemsByHashedAccountIdAsync(string hashedAccountId);

    /// <summary>
    /// Updates an existing file system item in the database.
    /// </summary>
    /// <param name="item">The file system item to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(FileSystemItem item);

    /// <summary>
    /// Deletes a file system item from the database.
    /// </summary>
    /// <param name="id">The ID of the item to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string id);
}
