using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for detecting file synchronization conflicts between local and remote files.
/// </summary>
public sealed class ConflictDetectionService : IConflictDetectionService
{
    private const double AllowedTimeDifference = 60.0;
    private const double OneHourInSeconds = 3600.0;
    private const double OneSecondThreshold = 1.0;

    private readonly ISyncConflictRepository _syncConflictRepository;
    private readonly IFileOperationLogRepository _fileOperationLogRepository;
    private readonly IDriveItemsRepository _driveItemsRepository;

    public ConflictDetectionService(
        ISyncConflictRepository syncConflictRepository,
        IFileOperationLogRepository fileOperationLogRepository,
        IDriveItemsRepository driveItemsRepository)
    {
        _syncConflictRepository = syncConflictRepository ?? throw new ArgumentNullException(nameof(syncConflictRepository));
        _fileOperationLogRepository = fileOperationLogRepository ?? throw new ArgumentNullException(nameof(fileOperationLogRepository));
        _driveItemsRepository = driveItemsRepository ?? throw new ArgumentNullException(nameof(driveItemsRepository));
    }

    /// <inheritdoc />
    public async Task<(bool HasConflict, FileMetadata? FileToDownload)> CheckKnownFileConflictAsync(
        HashedAccountId hashedAccountId,
        DriveItemEntity remoteFile,
        DriveItemEntity existingFile,
        Dictionary<string, FileMetadata> localFilesDict,
        string? localSyncPath,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        await DebugLog.InfoAsync("ConflictDetectionService.CheckKnownFileConflictAsync", hashedAccountId,
            $"Found file in DB: {remoteFile.RelativePath}, DB Status={existingFile.SyncStatus}", cancellationToken);

        var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
        var remoteHasChanged = ((!string.IsNullOrWhiteSpace(existingFile.CTag) ||
                                    timeDiff > OneHourInSeconds ||
                                    existingFile.Size != remoteFile.Size) && (existingFile.CTag != remoteFile.CTag)) ||
                                    remoteFile.SyncStatus == FileSyncStatus.SyncOnly;

        await DebugLog.InfoAsync("ConflictDetectionService.CheckKnownFileConflictAsync",
            hashedAccountId,
            $"Remote file check: {remoteFile.RelativePath} - DB CTag={existingFile.CTag}, Remote CTag={remoteFile.CTag}, " +
            $"DB Time={existingFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, " +
            $"Diff={timeDiff:F1}s, DB Size={existingFile.Size}, Remote Size={remoteFile.Size}, RemoteHasChanged={remoteHasChanged}",
            cancellationToken);

        if(!remoteHasChanged)
            return (false, null);

        var localFileHasChanged = CheckIfLocalFileHasChanged(remoteFile.RelativePath ?? "", existingFile, localFilesDict);

        if(localFileHasChanged)
        {
            FileMetadata localFile = localFilesDict[remoteFile.RelativePath ?? ""];
            await RecordSyncConflictAsync(hashedAccountId, remoteFile, localFile, sessionId, cancellationToken);
            return (true, null);
        }

        FileMetadata fileToDownload = CreateFileMetadataWithLocalPath(remoteFile, hashedAccountId, localSyncPath);
        return (false, fileToDownload);
    }

    /// <inheritdoc />
    public async Task<(bool HasConflict, FileMetadata? FileToDownload, FileMetadata? MatchedFile)> CheckFirstSyncFileConflictAsync(
        HashedAccountId hashedAccountId,
        DriveItemEntity remoteFile,
        Dictionary<string, FileMetadata> localFilesDict,
        string? localSyncPath,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        await DebugLog.InfoAsync("ConflictDetectionService.CheckFirstSyncFileConflictAsync", hashedAccountId,
            $"File not in DB: {remoteFile.RelativePath} - first sync or new file", cancellationToken);

        if(!localFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out FileMetadata? localFile))
        {
            FileMetadata fileToDownload = CreateFileMetadataWithLocalPath(remoteFile, hashedAccountId, localSyncPath);
            await DebugLog.InfoAsync("ConflictDetectionService.CheckFirstSyncFileConflictAsync", hashedAccountId,
                $"New remote file to download: {remoteFile.RelativePath}", cancellationToken);
            return (false, fileToDownload, null);
        }

        var timeDiff = Math.Abs((localFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
        var filesMatch = localFile.Size == remoteFile.Size && timeDiff <= AllowedTimeDifference;

        await DebugLog.InfoAsync("ConflictDetectionService.CheckFirstSyncFileConflictAsync",
            hashedAccountId,
            $"First sync compare: {remoteFile.RelativePath} - Local: Size={localFile.Size}, Time={localFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, " +
            $"Remote: Size={remoteFile.Size}, Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, TimeDiff={timeDiff:F1}s, Match={filesMatch}",
            cancellationToken);

        if(filesMatch)
        {
            await DebugLog.InfoAsync("ConflictDetectionService.CheckFirstSyncFileConflictAsync", hashedAccountId,
                $"File exists both places and matches: {remoteFile.RelativePath} - recording in DB", cancellationToken);
            FileMetadata matchedFile = localFile with
            {
                DriveItemId = remoteFile.DriveItemId,
                CTag = remoteFile.CTag,
                ETag = remoteFile.ETag,
                SyncStatus = FileSyncStatus.Synced,
                LastSyncDirection = null
            };
            return (false, null, matchedFile);
        }

        await DebugLog.InfoAsync("ConflictDetectionService.CheckFirstSyncFileConflictAsync", hashedAccountId,
            $"First sync CONFLICT: {remoteFile.RelativePath} - files differ (TimeDiff={timeDiff:F1}s, SizeMatch={localFile.Size == remoteFile.Size})",
            cancellationToken);
        await RecordSyncConflictAsync(hashedAccountId, remoteFile, localFile, sessionId, cancellationToken);
        return (true, null, null);
    }

    /// <inheritdoc />
    public bool CheckIfLocalFileHasChanged(
        string relativePath,
        DriveItemEntity existingFile,
        Dictionary<string, FileMetadata> localFilesDict)
    {
        if(!localFilesDict.TryGetValue(relativePath, out FileMetadata? localFile))
            return false;

        var localTimeDiff = Math.Abs((existingFile.LastModifiedUtc - localFile.LastModifiedUtc).TotalSeconds);
        return localTimeDiff > OneSecondThreshold || existingFile.Size != localFile.Size;
    }

    /// <inheritdoc />
    public async Task RecordSyncConflictAsync(
        HashedAccountId hashedAccountId,
        DriveItemEntity remoteFile,
        FileMetadata localFile,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        var conflict = SyncConflict.CreateUnresolvedConflict(
            hashedAccountId,
            remoteFile.RelativePath ?? "",
            localFile.LastModifiedUtc,
            remoteFile.LastModifiedUtc,
            localFile.Size,
            remoteFile.Size);

        SyncConflict? existingConflict = await _syncConflictRepository.GetByFilePathAsync(
            hashedAccountId,
            remoteFile.RelativePath ?? "",
            cancellationToken);

        if(existingConflict is null)
        {
            await _syncConflictRepository.AddAsync(conflict, cancellationToken);
        }

        await DebugLog.InfoAsync("ConflictDetectionService.RecordSyncConflictAsync", hashedAccountId,
            $"CONFLICT detected for {remoteFile.RelativePath}: local and remote both changed", cancellationToken);

        if(sessionId is not null)
        {
            var operationLog = FileOperationLog.CreateSyncConflictLog(
                sessionId,
                hashedAccountId,
                remoteFile.RelativePath ?? "",
                localFile.LocalPath,
                remoteFile.DriveItemId,
                localFile.LocalHash,
                localFile.Size,
                localFile.LastModifiedUtc,
                remoteFile.LastModifiedUtc);

            await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
        }

        await _driveItemsRepository.SaveBatchAsync(
            [localFile with { SyncStatus = FileSyncStatus.PendingDownload, IsSelected = true }],
            cancellationToken);
    }

    private static FileMetadata CreateFileMetadataWithLocalPath(DriveItemEntity remoteFile, HashedAccountId hashedAccountId, string? localSyncPath)
    {
        if(string.IsNullOrWhiteSpace(localSyncPath))
        {
            throw new ArgumentException("Local sync path cannot be null or empty when creating file metadata for download", nameof(localSyncPath));
        }

        var localFilePath = Path.Combine(localSyncPath, remoteFile.RelativePath?.TrimStart('/') ?? string.Empty);

        return new FileMetadata(
            remoteFile.DriveItemId,
            hashedAccountId,
            remoteFile.Name ?? string.Empty,
            remoteFile.RelativePath ?? string.Empty,
            remoteFile.Size,
            remoteFile.LastModifiedUtc,
            localFilePath,
            remoteFile.IsFolder,
            remoteFile.IsDeleted,
            remoteFile.IsSelected ?? false,
            remoteFile.RemoteHash,
            remoteFile.CTag,
            remoteFile.ETag,
            null,
            FileSyncStatus.PendingDownload,
            null);
    }
}
