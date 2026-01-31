using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

public sealed class EfSyncRepository(IDbContextFactory<SyncDbContext> dbContextFactory) : ISyncRepository
{
    public async Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken)
    {
        await using SyncDbContext db = dbContextFactory.CreateDbContext();

        return await db.DeltaTokens.Where(token => token.AccountId == accountId).OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken cancellationToken)
    {
        await using SyncDbContext db = dbContextFactory.CreateDbContext();
        cancellationToken.ThrowIfCancellationRequested();
        DeltaToken? existing = await db.DeltaTokens.FindAsync([token.Id], cancellationToken);
        if(existing is null)
            _ = db.DeltaTokens.Add(token);
        else
            db.Entry(existing).CurrentValues.SetValues(token);
        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyDriveItemsAsync(string accountId, IEnumerable<DriveItemRecord> items, CancellationToken cancellationToken)
    {
        await using SyncDbContext db = dbContextFactory.CreateDbContext();
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach(DriveItemRecord item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DriveItemRecord? existing = await db.DriveItems.FindAsync([item.Id], cancellationToken);
            if(existing is null)
                _ = db.DriveItems.Add(item);
            else
                db.Entry(existing).CurrentValues.SetValues(item);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
