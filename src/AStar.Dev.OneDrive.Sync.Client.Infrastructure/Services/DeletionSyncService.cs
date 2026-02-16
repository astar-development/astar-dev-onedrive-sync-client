using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for handling file and folder deletions during synchronization.
/// </summary>
public sealed class DeletionSyncService(
    IDriveItemsRepository driveItemsRepository,
    IGraphApiClient graphApiClient) : IDeletionSyncService
{
    /// <inheritdoc />
    public async Task ProcessRemoteToLocalDeletionsAsync(string accountId, IReadOnlyList<DriveItemEntity> existingFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("DeletionSyncService.ProcessRemoteToLocalDeletionsAsync", accountId, cancellationToken);

        List<DriveItemEntity> deletedFromOneDrive = SelectFilesDeletedFromOneDriveButSyncedLocally(
            existingFiles, remotePathsSet, localPathsSet);

        await DebugLog.InfoAsync("DeletionSyncService.ProcessRemoteToLocalDeletionsAsync", accountId,
            $"Remote deletion detection: {deletedFromOneDrive.Count} files to delete locally.",
            cancellationToken);

        foreach(DriveItemEntity file in deletedFromOneDrive)
        {
            try
            {
                await DebugLog.InfoAsync("DeletionSyncService.ProcessRemoteToLocalDeletionsAsync", accountId,
                    $"File deleted from OneDrive: {file.RelativePath} - deleting local copy at {file.LocalPath}",
                    cancellationToken);
                if(File.Exists(file.LocalPath))
                    File.Delete(file.LocalPath);

                await driveItemsRepository.DeleteAsync(file.DriveItemId, cancellationToken);
            }
            catch(Exception ex)
            {
                await DebugLog.ErrorAsync("DeletionSyncService.ProcessRemoteToLocalDeletionsAsync", accountId,
                    $"Failed to delete local file {file.RelativePath}: {ex.Message}. Continuing with other deletions.",
                    ex, cancellationToken);
            }
        }

        await DebugLog.ExitAsync("DeletionSyncService.ProcessRemoteToLocalDeletionsAsync", accountId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ProcessLocalToRemoteDeletionsAsync(string accountId, HashedAccountId hashedAccountId, List<FileMetadata> allLocalFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId, cancellationToken);

        List<FileMetadata> deletedLocally = GetFilesDeletedLocally(allLocalFiles, remotePathsSet, localPathsSet);

        await DebugLog.InfoAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId,
            $"Local deletion detection: {deletedLocally.Count} files to delete from OneDrive.",
            cancellationToken);

        foreach(FileMetadata file in deletedLocally)
        {
            await DebugLog.InfoAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId,
                $"Candidate for remote deletion: Path={file.RelativePath}, Id={file.DriveItemId}, SyncStatus={file.SyncStatus}, ExistsLocally={File.Exists(file.LocalPath)}, ExistsRemotely={remotePathsSet.Contains(file.RelativePath)}",
                cancellationToken);
        }

        foreach(FileMetadata file in deletedLocally)
        {
            try
            {
                await DebugLog.InfoAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId,
                    $"Deleting from OneDrive: Path={file.RelativePath}, Id={file.DriveItemId}, SyncStatus={file.SyncStatus}",
                    cancellationToken);
                await graphApiClient.DeleteFileAsync(accountId, hashedAccountId, file.DriveItemId, cancellationToken);
                await DebugLog.InfoAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId,
                    $"Deleted from OneDrive: Path={file.RelativePath}, Id={file.DriveItemId}",
                    cancellationToken);
                await driveItemsRepository.DeleteAsync(file.DriveItemId, cancellationToken);
            }
            catch(Exception ex)
            {
                await DebugLog.ErrorAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId,
                    $"Failed to delete from OneDrive {file.RelativePath}: {ex.Message}, continuing the sync...",
                    ex, cancellationToken);
            }
        }

        await DebugLog.ExitAsync("DeletionSyncService.ProcessLocalToRemoteDeletionsAsync", accountId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CleanupDatabaseRecordsAsync(string accountId, IEnumerable<DriveItemEntity> itemsToDelete, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("DeletionSyncService.CleanupDatabaseRecordsAsync", accountId, cancellationToken);

        foreach(DriveItemEntity fileToDelete in itemsToDelete)
            await driveItemsRepository.DeleteAsync(fileToDelete.DriveItemId, cancellationToken);

        await DebugLog.ExitAsync("DeletionSyncService.CleanupDatabaseRecordsAsync", accountId, cancellationToken);
    }

    private static List<FileMetadata> GetFilesDeletedLocally(List<FileMetadata> allLocalFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet) => [
                .. allLocalFiles
                .Where(f => !localPathsSet.Contains(f.RelativePath) &&
                            (remotePathsSet.Contains(f.RelativePath) || f.SyncStatus == FileSyncStatus.Synced) &&
                            !string.IsNullOrEmpty(f.DriveItemId))
            ];

    private static List<DriveItemEntity> SelectFilesDeletedFromOneDriveButSyncedLocally(IReadOnlyList<DriveItemEntity> existingFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet) => [
                .. existingFiles
                .Where(f => !remotePathsSet.Contains(f.RelativePath) &&
                            localPathsSet.Contains(f.RelativePath) &&
                            f.SyncStatus == FileSyncStatus.Synced)
            ];
}
