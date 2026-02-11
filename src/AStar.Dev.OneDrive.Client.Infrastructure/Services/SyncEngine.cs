using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using AStar.Dev.OneDrive.Client.Core;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Service for synchronizing files between local storage and OneDrive.
/// </summary>
/// <remarks>
///     Supports bidirectional sync with conflict detection and resolution.
///     Uses LastWriteWins strategy: when both local and remote files change, the newer timestamp wins.
/// </remarks>
public sealed partial class SyncEngine : ISyncEngine, IDisposable
{
    private const double AllowedTimeDifference = 60.0;
    private const double OneHourInSeconds = 3600.0;
    private const double OneSecondThreshold = 1.0;
    private readonly IAccountRepository _accountRepository;
    private readonly IDeletionSyncService _deletionSyncService;
    private readonly IDeltaProcessingService _deltaProcessingService;
    private readonly IDriveItemsRepository _driveItemsRepository;
    private readonly IFileOperationLogRepository _fileOperationLogRepository;
    private readonly IFileTransferService _fileTransferService;
    private readonly IGraphApiClient _graphApiClient;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly BehaviorSubject<SyncState> _progressSubject;
    private readonly IRemoteChangeDetector _remoteChangeDetector;
    private readonly ISyncConfigurationRepository _syncConfigurationRepository;
    private readonly ISyncConflictRepository _syncConflictRepository;
    private readonly ISyncSessionLogRepository _syncSessionLogRepository;
    private readonly List<(DateTimeOffset Timestamp, long Bytes)> _transferHistory = [];
    private string? _currentSessionId;
    private long _lastCompletedBytes;
    private DateTimeOffset _lastProgressUpdate = DateTime.UtcNow;
    private CancellationTokenSource? _syncCancellation;
    private int _syncInProgress;

    public SyncEngine(
        ILocalFileScanner localFileScanner,
        IRemoteChangeDetector remoteChangeDetector,
        IDriveItemsRepository fileMetadataRepository,
        ISyncConfigurationRepository syncConfigurationRepository,
        IAccountRepository accountRepository,
        IGraphApiClient graphApiClient,
        ISyncConflictRepository syncConflictRepository,
        ISyncSessionLogRepository syncSessionLogRepository,
        IFileOperationLogRepository fileOperationLogRepository,
        IDeltaProcessingService deltaProcessingService,
        IFileTransferService fileTransferService,
        IDeletionSyncService deletionSyncService)
    {
        _localFileScanner = localFileScanner ?? throw new ArgumentNullException(nameof(localFileScanner));
        _remoteChangeDetector = remoteChangeDetector ?? throw new ArgumentNullException(nameof(remoteChangeDetector));
        _driveItemsRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
        _syncConfigurationRepository = syncConfigurationRepository ?? throw new ArgumentNullException(nameof(syncConfigurationRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _syncConflictRepository = syncConflictRepository ?? throw new ArgumentNullException(nameof(syncConflictRepository));
        _syncSessionLogRepository = syncSessionLogRepository ?? throw new ArgumentNullException(nameof(syncSessionLogRepository));
        _fileOperationLogRepository = fileOperationLogRepository ?? throw new ArgumentNullException(nameof(fileOperationLogRepository));
        _deltaProcessingService = deltaProcessingService ?? throw new ArgumentNullException(nameof(deltaProcessingService));
        _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
        _deletionSyncService = deletionSyncService ?? throw new ArgumentNullException(nameof(deletionSyncService));
        var initialState = SyncState.CreateInitial(string.Empty);

        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }

    public void Dispose()
    {
        _syncCancellation?.Dispose();
        _progressSubject.Dispose();
    }

    /// <inheritdoc />
    public IObservable<SyncState> Progress => _progressSubject;

    /// <inheritdoc />
    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.SyncEngine.StartSync, accountId, cancellationToken);
        DebugLogContext.SetAccountId(accountId);

        if(SyncIsAlreadyRunning())
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Sync already in progress for account {accountId}, ignoring duplicate request. Exiting", cancellationToken);
            return;
        }

        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            ResetTrackingDetails();

            AccountInfo? account = await ValidateAndGetAccountAsync(accountId, cancellationToken);
            if(account is null)
            {
                ReportProgress(accountId, SyncStatus.Failed);
                return;
            }

            await ProcessDeltaChangesAsync(accountId, cancellationToken);

            IReadOnlyList<DriveItemEntity> folders = await GetSelectedFoldersAsync(accountId, cancellationToken);
            if(folders.Count == 0)
            {
                ReportProgress(accountId, SyncStatus.Idle);
                return;
            }

            await InitializeSyncSessionAsync(accountId, account.EnableDetailedSyncLogging, cancellationToken);

            List<FileMetadata> allLocalFiles = await ScanLocalFilesAsync(accountId, folders, account);
            var existingFilesDict = folders.ToDictionary(f => f.RelativePath ?? "", f => f);
            var localFilesDict = allLocalFiles.ToDictionary(f => f.RelativePath ?? "", f => f);

            List<FileMetadata> filesToUpload = await DetectFilesToUploadAsync(
                accountId, allLocalFiles, existingFilesDict, folders, cancellationToken);

            var remotePathsSet = folders.Select(f => f.RelativePath).ToHashSet();
            var localPathsSet = allLocalFiles.Select(f => f.RelativePath).ToHashSet();

            (List<FileMetadata> filesToDownload, var conflictCount, HashSet<string> conflictPaths) =
                await DetectFilesToDownloadAndConflictsAsync(
                    accountId, folders, existingFilesDict, localFilesDict, account, cancellationToken);

            await _deletionSyncService.ProcessRemoteToLocalDeletionsAsync(
                accountId, folders, remotePathsSet, localPathsSet, cancellationToken);

            await _deletionSyncService.ProcessLocalToRemoteDeletionsAsync(
                accountId, allLocalFiles, remotePathsSet, localPathsSet, cancellationToken);

            filesToUpload = FilterUploadsByDeletionsAndConflicts(
                filesToUpload, folders, remotePathsSet, conflictPaths);

            (filesToDownload, var totalFiles, var totalBytes, var uploadBytes, var downloadBytes) =
                await CalculateSyncSummaryAsync(
                    accountId, filesToUpload, filesToDownload, cancellationToken);

            var filesDeleted = 0;
            ReportProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes,
                filesDeleted: filesDeleted, conflictsDetected: conflictCount);

            var completedFiles = 0;
            var completedBytes = 0L;

            (completedFiles, completedBytes) = await _fileTransferService.ExecuteUploadsAsync(
                accountId, folders, filesToUpload, account.MaxParallelUpDownloads,
                conflictCount, totalFiles, totalBytes, uploadBytes, completedFiles, completedBytes, _currentSessionId,
                ReportProgress, _syncCancellation, cancellationToken);

            ResetTrackingDetails(completedBytes);

            (completedFiles, completedBytes) = await _fileTransferService.ExecuteDownloadsAsync(
                accountId, folders, filesToDownload, account.MaxParallelUpDownloads,
                conflictCount, totalFiles, totalBytes, uploadBytes, downloadBytes, completedFiles, completedBytes, _currentSessionId,
                ReportProgress, _syncCancellation, cancellationToken);

            ReportProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes,
                completedBytes, filesDeleted: filesDeleted, conflictsDetected: conflictCount);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                $"Sync completed: {totalFiles} files, {completedBytes} bytes", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", accountId, cancellationToken);

            await FinalizeSyncSessionAsync(_currentSessionId, filesToUpload.Count,
                filesToDownload.Count, filesDeleted, conflictCount, completedBytes, account, cancellationToken);
        }
        catch(OperationCanceledException)
        {
            await HandleSyncCancelledAsync(_currentSessionId, cancellationToken);
            ReportProgress(accountId, SyncStatus.Paused);
            throw;
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", accountId,
                $"Sync failed: {ex.Message}", ex, cancellationToken);
            await HandleSyncFailedAsync(_currentSessionId, cancellationToken);
            ReportProgress(accountId, SyncStatus.Failed);
            throw;
        }
        finally
        {
            DebugLogContext.Clear();
            _ = Interlocked.Exchange(ref _syncInProgress, 0);
        }
    }

    private async Task<AccountInfo?> ValidateAndGetAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        AccountInfo? account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        return account;
    }

    private async Task ProcessDeltaChangesAsync(string accountId, CancellationToken cancellationToken)
    {
        DeltaToken? token = await _deltaProcessingService.GetDeltaTokenAsync(accountId, cancellationToken);
        (DeltaToken? finalDelta, var pageCount, var totalItemsProcessed) =
            await _deltaProcessingService.ProcessDeltaPagesAsync(
                accountId,
                token,
                _progressSubject.OnNext,
                cancellationToken);
        await _deltaProcessingService.SaveDeltaTokenAsync(finalDelta, cancellationToken);
        await DebugLog.EntryAsync("SyncEngine.StartSyncAsync", accountId, cancellationToken);
    }

    private async Task<IReadOnlyList<DriveItemEntity>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken)
    {
        IReadOnlyList<DriveItemEntity> folders = await _syncConfigurationRepository
            .GetSelectedItemsByAccountIdAsync(accountId, cancellationToken);

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
            $"Starting sync with {folders.Count} selected folders: {string.Join(", ", folders)}",
            cancellationToken);

        return folders;
    }

    private async Task InitializeSyncSessionAsync(string accountId, bool enableDetailedSyncLogging, CancellationToken cancellationToken)
    {
        if(enableDetailedSyncLogging)
        {
            var sessionLog = SyncSessionLog.CreateInitialRunning(accountId);
            await _syncSessionLogRepository.AddAsync(sessionLog, cancellationToken);
            _currentSessionId = sessionLog.Id;
        }
        else
        {
            _currentSessionId = null;
        }
    }

    private async Task<List<FileMetadata>> ScanLocalFilesAsync(string accountId, IReadOnlyList<DriveItemEntity> selectedFolders, AccountInfo account)
    {
        var allLocalFiles = new List<FileMetadata>();
        foreach(DriveItemEntity driveItem in selectedFolders.Where(f => f.IsFolder))
        {
            var localFolderPath = Path.Combine(account.LocalSyncPath, driveItem.RelativePath.TrimStart('/'));
            IReadOnlyList<FileMetadata> localFiles = await _localFileScanner.ScanFolderAsync(
                accountId,
                localFolderPath,
                driveItem.RelativePath,
                _syncCancellation?.Token ?? CancellationToken.None);
            if(localFiles?.Count > 0)
                allLocalFiles.AddRange(localFiles);
        }
        return allLocalFiles.DistinctBy(f => f.RelativePath).ToList();
    }

    private static async Task<List<FileMetadata>> DetectFilesToUploadAsync(string accountId, List<FileMetadata> allLocalFiles, Dictionary<string, DriveItemEntity> existingFilesDict,
        IReadOnlyList<DriveItemEntity> folders, CancellationToken cancellationToken)
    {
        var filesToUpload = new List<FileMetadata>();

        foreach(FileMetadata localFile in allLocalFiles)
        {
            if(existingFilesDict.TryGetValue(localFile.RelativePath, out DriveItemEntity? existingFile))
            {
                if(existingFile.SyncStatus is FileSyncStatus.PendingUpload or FileSyncStatus.Failed)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                        $"File needs upload (status={existingFile.SyncStatus}): {localFile.Name}", cancellationToken);
                    var fileToUpload = new FileMetadata(
                        existingFile.DriveItemId,
                        accountId,
                        existingFile.Name ?? string.Empty,
                        existingFile.RelativePath ?? string.Empty,
                        existingFile.Size,
                        existingFile.LastModifiedUtc,
                        existingFile.LocalPath ?? string.Empty,
                        IsFolder: false,
                        IsDeleted: false);
                    filesToUpload.Add(fileToUpload);
                }
                else
                {
                    var bothHaveHashes = !string.IsNullOrEmpty(existingFile.LocalHash) &&
                        !string.IsNullOrEmpty(localFile.LocalHash);

                    bool hasChanged;
                    if(bothHaveHashes)
                    {
                        hasChanged = existingFile.LocalHash != localFile.LocalHash;
                        if(hasChanged)
                        {
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                                $"File marked as changed: {localFile.Name} - Hash changed (DB: {existingFile.LocalHash}, Local: {localFile.LocalHash})",
                                cancellationToken);
                        }
                    }
                    else
                    {
                        hasChanged = existingFile.Size != localFile.Size;
                        if(hasChanged)
                        {
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                                $"File marked as changed: {localFile.Name} - Size changed (DB: {existingFile.Size}, Local: {localFile.Size})",
                                cancellationToken);
                        }
                    }

                    if(hasChanged)
                        filesToUpload.Add(localFile);
                }
            }
            else if(folders.FirstOrDefault(f => f.RelativePath == localFile.RelativePath) is null)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                    $"New local file to upload: {localFile.Name}", cancellationToken);
                filesToUpload.Add(localFile);
            }
        }

        return filesToUpload;
    }

    private async Task<(List<FileMetadata> FilesToDownload, int ConflictCount, HashSet<string> ConflictPaths)> DetectFilesToDownloadAndConflictsAsync(
        string accountId, IReadOnlyList<DriveItemEntity> folders, Dictionary<string, DriveItemEntity> existingFilesDict, Dictionary<string, FileMetadata> localFilesDict,
        AccountInfo account, CancellationToken cancellationToken)
    {
        var filesToDownload = new List<FileMetadata>();
        var conflictCount = 0;
        var conflictPaths = new HashSet<string>();
        var filesToRecordWithoutTransfer = new List<FileMetadata>();

        foreach(DriveItemEntity remoteFile in folders)
        {
            if(existingFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out DriveItemEntity? existingFile))
            {
                (var HasConflict, FileMetadata? FileToDownload) = await ProcessKnownRemoteFileAsync(accountId, remoteFile, existingFile, localFilesDict, account, cancellationToken);
                if(HasConflict)
                {
                    conflictCount++;
                    _ = conflictPaths.Add(remoteFile.RelativePath ?? "");
                }
                else if(FileToDownload is not null)
                {
                    filesToDownload.Add(FileToDownload);
                }
            }
            else
            {
                (var HasConflict, FileMetadata? FileToDownload, FileMetadata? MatchedFile) = await ProcessFirstSyncFileAsync(accountId, remoteFile, localFilesDict, account, cancellationToken);
                if(HasConflict)
                {
                    conflictCount++;
                    _ = conflictPaths.Add(remoteFile.RelativePath ?? "");
                }
                else if(FileToDownload is not null)
                {
                    filesToDownload.Add(FileToDownload);
                }
                else if(MatchedFile is not null)
                {
                    filesToRecordWithoutTransfer.Add(MatchedFile);
                }
            }
        }

        return (filesToDownload, conflictCount, conflictPaths);
    }

    private async Task<(bool HasConflict, FileMetadata? FileToDownload)> ProcessKnownRemoteFileAsync(string accountId, DriveItemEntity remoteFile,
        DriveItemEntity existingFile, Dictionary<string, FileMetadata> localFilesDict, AccountInfo account, CancellationToken cancellationToken)
    {
        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Found file in DB: {remoteFile.RelativePath}, DB Status={existingFile.SyncStatus}", cancellationToken);

        var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
        var remoteHasChanged = ((!string.IsNullOrWhiteSpace(existingFile.CTag) ||
                                    timeDiff > OneHourInSeconds ||
                                    existingFile.Size != remoteFile.Size) && (existingFile.CTag != remoteFile.CTag)) || remoteFile.SyncStatus == FileSyncStatus.SyncOnly;

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync",
            accountId, $"Remote file check: {remoteFile.RelativePath} - DB CTag={existingFile.CTag}, Remote CTag={remoteFile.CTag}, DB Time={existingFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Diff={timeDiff:F1}s, DB Size={existingFile.Size}, Remote Size={remoteFile.Size}, RemoteHasChanged={remoteHasChanged}",
            cancellationToken);

        if(!remoteHasChanged)
            return (false, null);

        var localFileHasChanged = CheckIfLocalFileHasChanged(remoteFile.RelativePath ?? "", existingFile, localFilesDict);

        if(localFileHasChanged)
        {
            FileMetadata localFile = localFilesDict[remoteFile.RelativePath ?? ""];
            await RecordSyncConflictAsync(accountId, remoteFile, localFile, cancellationToken);
            return (true, null);
        }

        FileMetadata fileToDownload = CreateFileMetadataWithLocalPath(remoteFile, accountId, account.LocalSyncPath);
        return (false, fileToDownload);
    }

    private async Task<(bool HasConflict, FileMetadata? FileToDownload, FileMetadata? MatchedFile)> ProcessFirstSyncFileAsync(
        string accountId, DriveItemEntity remoteFile, Dictionary<string, FileMetadata> localFilesDict, AccountInfo account, CancellationToken cancellationToken)
    {
        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"File not in DB: {remoteFile.RelativePath} - first sync or new file", cancellationToken);

        if(!localFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out FileMetadata? localFile))
        {
            FileMetadata fileToDownload = CreateFileMetadataWithLocalPath(remoteFile, accountId, account.LocalSyncPath);
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"New remote file to download: {remoteFile.RelativePath}", cancellationToken);
            return (false, fileToDownload, null);
        }

        var timeDiff = Math.Abs((localFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
        var filesMatch = localFile.Size == remoteFile.Size && timeDiff <= AllowedTimeDifference;

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync",
            accountId, $"First sync compare: {remoteFile.RelativePath} - Local: Size={localFile.Size}, Time={localFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote: Size={remoteFile.Size}, Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, TimeDiff={timeDiff:F1}s, Match={filesMatch}",
            cancellationToken);

        if(filesMatch)
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"File exists both places and matches: {remoteFile.RelativePath} - recording in DB", cancellationToken);
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

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
            $"First sync CONFLICT: {remoteFile.RelativePath} - files differ (TimeDiff={timeDiff:F1}s, SizeMatch={localFile.Size == remoteFile.Size})", cancellationToken);
        await RecordSyncConflictAsync(accountId, remoteFile, localFile, cancellationToken);
        return (true, null, null);
    }

    private static bool CheckIfLocalFileHasChanged(string relativePath, DriveItemEntity existingFile, Dictionary<string, FileMetadata> localFilesDict)
    {
        if(!localFilesDict.TryGetValue(relativePath, out FileMetadata? localFile))
            return false;

        var localTimeDiff = Math.Abs((existingFile.LastModifiedUtc - localFile.LastModifiedUtc).TotalSeconds);
        return localTimeDiff > OneSecondThreshold || existingFile.Size != localFile.Size;
    }

    private async Task RecordSyncConflictAsync(string accountId, DriveItemEntity remoteFile, FileMetadata localFile, CancellationToken cancellationToken)
    {
        var conflict = SyncConflict.CreateUnresolvedConflict(accountId, remoteFile.RelativePath ?? "", localFile.LastModifiedUtc,
            remoteFile.LastModifiedUtc, localFile.Size, remoteFile.Size);

        SyncConflict? existingConflict = await _syncConflictRepository.GetByFilePathAsync(accountId, remoteFile.RelativePath ?? "", cancellationToken);
        if(existingConflict is null)
        {
            await _syncConflictRepository.AddAsync(conflict, cancellationToken);
        }

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"CONFLICT detected for {remoteFile.RelativePath}: local and remote both changed", cancellationToken);

        if(_currentSessionId is not null)
        {
            var operationLog = FileOperationLog.CreateSyncConflictLog(_currentSessionId, accountId, remoteFile.RelativePath ?? "", localFile.LocalPath,
                remoteFile.DriveItemId, localFile.LocalHash, localFile.Size, localFile.LastModifiedUtc, remoteFile.LastModifiedUtc);
            await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
            await _driveItemsRepository.SaveBatchAsync([localFile with { SyncStatus = FileSyncStatus.PendingDownload, IsSelected = true }], cancellationToken);
        }
    }

    private static FileMetadata CreateFileMetadataWithLocalPath(DriveItemEntity remoteFile, string accountId, string localSyncPath)
    {
        var localFilePath = Path.Combine(localSyncPath, remoteFile.RelativePath?.TrimStart('/') ?? "");
        return new FileMetadata(
            remoteFile.DriveItemId,
            accountId,
            remoteFile.Name ?? string.Empty,
            remoteFile.RelativePath ?? string.Empty,
            remoteFile.Size,
            remoteFile.LastModifiedUtc,
            localFilePath,
            IsFolder: false,
            IsDeleted: false);
    }

    private static List<FileMetadata> FilterUploadsByDeletionsAndConflicts(List<FileMetadata> filesToUpload, IReadOnlyList<DriveItemEntity> folders,
        HashSet<string> remotePathsSet, HashSet<string> conflictPaths)
    {
        var deletedPaths = folders
            .Where(f => !remotePathsSet.Contains(f.RelativePath))
            .Select(f => f.RelativePath)
            .ToHashSet();

        return [.. filesToUpload.Where(f =>
            !deletedPaths.Contains(f.RelativePath) &&
            !conflictPaths.Contains(f.RelativePath))];
    }

    private static async Task<(
        List<FileMetadata> FilesToDownload,
        int TotalFiles,
        long TotalBytes,
        long UploadBytes,
        long DownloadBytes)> CalculateSyncSummaryAsync(
            string accountId,
            List<FileMetadata> filesToUpload,
            List<FileMetadata> filesToDownload,
            CancellationToken cancellationToken)
    {
        var totalFiles = filesToUpload.Count + filesToDownload.Count;
        var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
        var uploadBytes = filesToUpload.Sum(f => f.Size);
        var downloadBytes = filesToDownload.Sum(f => f.Size);

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
            $"Sync summary: {filesToDownload.Count} to download, {filesToUpload.Count} to upload",
            cancellationToken);

        (filesToDownload, totalFiles, totalBytes, downloadBytes) =
            await RemoveDuplicatesFromDownloadList(filesToUpload, filesToDownload, totalFiles, totalBytes, downloadBytes, accountId, cancellationToken);

        uploadBytes = filesToUpload.Sum(f => f.Size);

        return (filesToDownload, totalFiles, totalBytes, uploadBytes, downloadBytes);
    }



    /// <inheritdoc />
    public Task StopSyncAsync()
    {
        _syncCancellation?.Cancel();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default)
        => await _syncConflictRepository.GetUnresolvedByAccountIdAsync(accountId, cancellationToken);

    private static async Task<(List<FileMetadata> filesToDownload, int totalFiles, long totalBytes, long downloadBytes)> RemoveDuplicatesFromDownloadList(List<FileMetadata> filesToUpload,
        List<FileMetadata> filesToDownload, int totalFiles, long totalBytes, long downloadBytes, string accountId, CancellationToken cancellationToken)
    {
        var duplicateDownloads = filesToDownload.GroupBy(f => f.RelativePath).Where(g => g.Count() > 1).ToList();
        if(duplicateDownloads.Count > 0)
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"WARNING: Found {duplicateDownloads.Count} duplicate paths in download list!", cancellationToken);
            foreach(IGrouping<string, FileMetadata>? dup in duplicateDownloads)
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"  Duplicate: {dup.Key} appears {dup.Count()} times", cancellationToken);

            filesToDownload = [.. filesToDownload.GroupBy(f => f.RelativePath).Select(g => g.First())];
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"After deduplication: {filesToDownload.Count} files to download", cancellationToken);

            totalFiles = filesToUpload.Count + filesToDownload.Count;
            totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            downloadBytes = filesToDownload.Sum(f => f.Size);
        }

        return (filesToDownload, totalFiles, totalBytes, downloadBytes);
    }

    private async Task<List<FileMetadata>> GetAllLocalFiles(string accountId, IReadOnlyList<DriveItemEntity> selectedFolders, AccountInfo account)
    {
        var allLocalFiles = new List<FileMetadata>();
        foreach(DriveItemEntity driveItem in selectedFolders.Where(f => f.IsFolder))
        {
            var localFolderPath = Path.Combine(account.LocalSyncPath, driveItem.RelativePath.TrimStart('/'));
            IReadOnlyList<FileMetadata> localFiles = await _localFileScanner.ScanFolderAsync(
                accountId,
                localFolderPath,
                driveItem.RelativePath,
                _syncCancellation?.Token ?? CancellationToken.None);
            if(localFiles?.Count > 0)
                allLocalFiles.AddRange(localFiles);
        }

        return allLocalFiles.DistinctBy(f => f.RelativePath).ToList();
    }

    private void ResetTrackingDetails(long completedBytes = 0)
    {
        _transferHistory.Clear();
        _lastProgressUpdate = DateTime.UtcNow;
        _lastCompletedBytes = completedBytes;
    }

    private bool SyncIsAlreadyRunning() => Interlocked.CompareExchange(ref _syncInProgress, 1, 0) != 0;

    private async Task FinalizeSyncSessionAsync(string? sessionId, int uploadCount, int downloadCount, int deleteCount, int conflictCount, long completedBytes, AccountInfo account,
        CancellationToken cancellationToken)
    {
        if(sessionId is null)
            return;

        try
        {
            SyncSessionLog? session = await _syncSessionLogRepository.GetByIdAsync(sessionId, cancellationToken);
            if(session is not null)
            {
                SyncSessionLog updatedSession = session with
                {
                    CompletedUtc = DateTime.UtcNow,
                    Status = SyncStatus.Completed,
                    FilesUploaded = uploadCount,
                    FilesDownloaded = downloadCount,
                    FilesDeleted = deleteCount,
                    ConflictsDetected = conflictCount,
                    TotalBytes = completedBytes
                };
                await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
            }

            await UpdateLastAccountSyncAsync(account, cancellationToken);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"[SyncEngine] Failed to finalize sync session log: {ex.Message}");
        }
        finally
        {
            _currentSessionId = null;
        }
    }

    private async Task HandleSyncCancelledAsync(string? sessionId, CancellationToken cancellationToken)
    {
        if(sessionId is null)
            return;

        SyncSessionLog? session = await _syncSessionLogRepository.GetByIdAsync(sessionId, cancellationToken);
        if(session is not null)
        {
            SyncSessionLog updatedSession = session with { CompletedUtc = DateTime.UtcNow, Status = SyncStatus.Paused };
            await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
        }
    }

    private async Task HandleSyncFailedAsync(string? sessionId, CancellationToken cancellationToken)
    {
        if(sessionId is null)
            return;

        SyncSessionLog? session = await _syncSessionLogRepository.GetByIdAsync(sessionId, cancellationToken);
        if(session is not null)
        {
            SyncSessionLog updatedSession = session with { CompletedUtc = DateTime.UtcNow, Status = SyncStatus.Failed };
            await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
        }
    }

    private async Task UpdateLastAccountSyncAsync(AccountInfo account, CancellationToken cancellationToken)
    {
        AccountInfo lastSyncUpdate = account with { LastSyncUtc = DateTime.UtcNow };

        await _accountRepository.UpdateAsync(lastSyncUpdate, cancellationToken);
    }

    public void ReportProgress(
        string accountId,
        SyncStatus status,
        int totalFiles = 0,
        int completedFiles = 0,
        long totalBytes = 0,
        long completedBytes = 0,
        int filesDownloading = 0,
        int filesUploading = 0,
        int filesDeleted = 0,
        int conflictsDetected = 0,
        string? currentScanningFolder = null,
        long? phaseTotalBytes = null)
    {
        DateTimeOffset now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastProgressUpdate).TotalSeconds;

        double megabytesPerSecond = 0;
        if(elapsedSeconds > 0.1)
        {
            var bytesDelta = completedBytes - _lastCompletedBytes;
            if(bytesDelta > 0)
            {
                var megabytesDelta = bytesDelta / (1024.0 * 1024.0);
                megabytesPerSecond = megabytesDelta / elapsedSeconds;

                _transferHistory.Add((now, completedBytes));
                if(_transferHistory.Count > 10)
                    _transferHistory.RemoveAt(0);

                if(_transferHistory.Count >= 2)
                {
                    var totalElapsed = (now - _transferHistory[0].Timestamp).TotalSeconds;
                    var totalTransferred = completedBytes - _transferHistory[0].Bytes;
                    if(totalElapsed > 0)
                        megabytesPerSecond = totalTransferred / (1024.0 * 1024.0) / totalElapsed;
                }

                _lastProgressUpdate = now;
                _lastCompletedBytes = completedBytes;
            }
        }

        int? estimatedSecondsRemaining = null;
        var bytesForEta = phaseTotalBytes ?? totalBytes;
        if(megabytesPerSecond > 0.01 && completedBytes < bytesForEta)
        {
            var remainingBytes = bytesForEta - completedBytes;
            var remainingMegabytes = remainingBytes / (1024.0 * 1024.0);
            estimatedSecondsRemaining = (int)Math.Ceiling(remainingMegabytes / megabytesPerSecond);
        }

        var progress = new SyncState(
            accountId,
            status,
            totalFiles,
            completedFiles,
            totalBytes,
            completedBytes,
            filesDownloading,
            filesUploading,
            filesDeleted,
            conflictsDetected,
            megabytesPerSecond,
            estimatedSecondsRemaining,
            currentScanningFolder,
            now);

        _progressSubject.OnNext(progress);
    }

    /// <summary>
    ///     Formats a folder path for display by removing Graph API prefixes.
    /// </summary>
    public static string? FormatScanningFolderForDisplay(string? folderPath)
    {
        if(string.IsNullOrEmpty(folderPath))
            return folderPath;

        var cleaned = MyRegex().Replace(folderPath, string.Empty);

        if(cleaned.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned["/drive/root:".Length..];

        if(!string.IsNullOrEmpty(cleaned) && !cleaned.StartsWith('/'))
            cleaned = "/" + cleaned;

        return $"OneDrive: {cleaned}";
    }

    [GeneratedRegex(@"^/drives/[^/]+/root:")]
    private static partial Regex MyRegex();
}
