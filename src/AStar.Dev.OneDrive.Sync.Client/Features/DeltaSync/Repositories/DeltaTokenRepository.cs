using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Repositories;

/// <summary>
/// Repository implementation for managing DeltaToken persistence using EF Core.
/// </summary>
public class DeltaTokenRepository(OneDriveSyncDbContext context) : IDeltaTokenRepository
{
    private readonly OneDriveSyncDbContext _context = context;

    /// <inheritdoc />
    public async Task<DeltaToken?> GetByAccountAndDriveAsync(string hashedAccountId, string driveName)
    {
        return await _context.DeltaTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(dt => dt.HashedAccountId == hashedAccountId && dt.DriveName == driveName);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeltaToken>> GetAllByAccountAsync(string hashedAccountId)
    {
        return await _context.DeltaTokens
            .AsNoTracking()
            .Where(dt => dt.HashedAccountId == hashedAccountId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SaveAsync(DeltaToken deltaToken)
    {
        DeltaToken? existing = await _context.DeltaTokens
            .FirstOrDefaultAsync(dt => dt.Id == deltaToken.Id);

        if (existing is null)
        {
            _ = await _context.DeltaTokens.AddAsync(deltaToken);
        }
        else
        {
            existing.Token = deltaToken.Token;
            existing.LastSyncAt = deltaToken.LastSyncAt;
            _ = _context.DeltaTokens.Update(existing);
        }

        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id)
    {
        DeltaToken? deltaToken = await _context.DeltaTokens.FindAsync(id);
        if (deltaToken is not null)
        {
            _ = _context.DeltaTokens.Remove(deltaToken);
            _ = await _context.SaveChangesAsync();
        }
    }
}
