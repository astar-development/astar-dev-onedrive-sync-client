using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.Services;

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
    private readonly IAccountRepository _accountRepository;
    private readonly IDeltaPageProcessor _deltaPageProcessor;
    private readonly IDriveItemsRepository _driveItemsRepository;
    private readonly IFileOperationLogRepository _fileOperationLogRepository;
    private readonly IGraphApiClient _graphApiClient;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly BehaviorSubject<SyncState> _progressSubject;
    private readonly IRemoteChangeDetector _remoteChangeDetector;
    private readonly ISyncConfigurationRepository _syncConfigurationRepository;
    private readonly ISyncConflictRepository _syncConflictRepository;
    private readonly ISyncRepository _syncRepository;
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
        ISyncRepository syncRepository,
        IDeltaPageProcessor deltaPageProcessor)
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
        _syncRepository = syncRepository;
        _deltaPageProcessor = deltaPageProcessor;
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

            AccountInfo? account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
            if(account is null)
            {
                ReportProgress(accountId, SyncStatus.Failed);
                return;
            }

            DeltaToken? token = await _syncRepository.GetDeltaTokenAsync(accountId, cancellationToken);

            (DeltaToken? finalDelta, var pageCount, var totalItemsProcessed) = await _deltaPageProcessor.ProcessAllDeltaPagesAsync(accountId, token ?? new(accountId, "", "", DateTimeOffset.UtcNow), _progressSubject.OnNext, cancellationToken);
            await _syncRepository.SaveOrUpdateDeltaTokenAsync(finalDelta, cancellationToken);

            await DebugLog.EntryAsync("SyncEngine.StartSyncAsync", accountId, cancellationToken);

            IReadOnlyList<DriveItemEntity> folders = await _syncConfigurationRepository.GetSelectedItemsByAccountIdAsync(accountId, cancellationToken);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Starting sync with {folders.Count} selected folders: {string.Join(", ", folders)}", cancellationToken);
            if(folders.Count == 0)
            {
                ReportProgress(accountId, SyncStatus.Idle);
                return;
            }

            if(account.EnableDetailedSyncLogging)
            {
                var sessionLog = SyncSessionLog.CreateInitialRunning(accountId);
                await _syncSessionLogRepository.AddAsync(sessionLog, cancellationToken);
                _currentSessionId = sessionLog.Id;
            }
            else
            {
                _currentSessionId = null;
            }

            List<FileMetadata> allLocalFiles = await GetAllLocalFiles(accountId, folders, account);
            var existingFilesDict = folders.ToDictionary(f => f.RelativePath ?? "", f => f);

            var localFilesDict = allLocalFiles.ToDictionary(f => f.RelativePath ?? "", f => f);

            var filesToUpload = new List<FileMetadata>();
            foreach(FileMetadata localFile in allLocalFiles)
            {
                if(existingFilesDict.TryGetValue(localFile.RelativePath, out DriveItemEntity? existingFile))
                {
                    if(existingFile.SyncStatus is FileSyncStatus.PendingUpload or FileSyncStatus.Failed)
                    {
                        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"File needs upload (status={existingFile.SyncStatus}): {localFile.Name}", cancellationToken);
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
                        var bothHaveHashes = !string.IsNullOrEmpty(existingFile.LocalHash) && !string.IsNullOrEmpty(localFile.LocalHash);

                        bool hasChanged;
                        if(bothHaveHashes)
                        {
                            hasChanged = existingFile.LocalHash != localFile.LocalHash;
                            if(hasChanged)
                            {
                                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync",
                                    accountId, $"File marked as changed: {localFile.Name} - Hash changed (DB: {existingFile.LocalHash}, Local: {localFile.LocalHash})", cancellationToken);
                            }
                        }
                        else
                        {
                            hasChanged = existingFile.Size != localFile.Size;
                            if(hasChanged)
                            {
                                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"File marked as changed: {localFile.Name} - Size changed (DB: {existingFile.Size}, Local: {localFile.Size})",
                                    cancellationToken);
                            }
                        }

                        if(hasChanged)
                            filesToUpload.Add(localFile);
                    }
                }
                else if(folders.FirstOrDefault(f => f.RelativePath == localFile.RelativePath) is null)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"New local file to upload: {localFile.Name}", cancellationToken);
                    filesToUpload.Add(localFile);
                }
            }

            var filesToDownload = new List<FileMetadata>();
            var remotePathsSet = folders.Select(f => f.RelativePath).ToHashSet();
            var conflictCount = 0;
            var conflictPaths = new HashSet<string>();
            var filesToRecordWithoutTransfer = new List<FileMetadata>();

            foreach(DriveItemEntity remoteFile in folders)
            {
                if(existingFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out DriveItemEntity? existingFile))
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Found file in DB: {remoteFile.RelativePath}, DB Status={existingFile.SyncStatus}", cancellationToken);
                    var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                    var remoteHasChanged = ((!string.IsNullOrWhiteSpace(existingFile.CTag) ||
                                            timeDiff > 3600.0 ||
                                            existingFile.Size != remoteFile.Size) && (existingFile.CTag != remoteFile.CTag)) || remoteFile.SyncStatus == FileSyncStatus.SyncOnly;

                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync",
                        accountId, $"Remote file check: {remoteFile.RelativePath} - DB CTag={existingFile.CTag}, Remote CTag={remoteFile.CTag}, DB Time={existingFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Diff={timeDiff:F1}s, DB Size={existingFile.Size}, Remote Size={remoteFile.Size}, RemoteHasChanged={remoteHasChanged}",
                        cancellationToken);

                    if(remoteHasChanged)
                    {
                        var localFileHasChanged = false;

                        if(localFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out FileMetadata? localFile))
                        {
                            var localTimeDiff = Math.Abs((existingFile.LastModifiedUtc - localFile.LastModifiedUtc).TotalSeconds);
                            localFileHasChanged = localTimeDiff > 1.0 || existingFile.Size != localFile.Size;
                        }

                        if(localFileHasChanged)
                        {
                            FileMetadata localFileFromDict = localFilesDict[remoteFile.RelativePath ?? ""];
                            var conflict = SyncConflict.CreateUnresolvedConflict(accountId, remoteFile.RelativePath ?? "", localFileFromDict.LastModifiedUtc, remoteFile.LastModifiedUtc, localFileFromDict.Size,
                                remoteFile.Size);

                            SyncConflict? existingConflict = await _syncConflictRepository.GetByFilePathAsync(accountId, remoteFile.RelativePath ?? "", cancellationToken);
                            if(existingConflict is null)
                            {
                                await _syncConflictRepository.AddAsync(conflict, cancellationToken);
                                conflictCount++;
                            }
                            else
                            {
                                conflictCount++;
                            }

                            _ = conflictPaths.Add(remoteFile.RelativePath ?? "");
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"CONFLICT detected for {remoteFile.RelativePath}: local and remote both changed", cancellationToken);

                            if(_currentSessionId is not null)
                            {
                                var operationLog = FileOperationLog.CreateSyncConflictLog(_currentSessionId, accountId, remoteFile.RelativePath ?? "", localFileFromDict.LocalPath, remoteFile.DriveItemId,
                                    localFileFromDict.LocalHash, localFileFromDict.Size, localFileFromDict.LastModifiedUtc, remoteFile.LastModifiedUtc);
                                await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                                await _driveItemsRepository.SaveBatchAsync([localFileFromDict with { SyncStatus = FileSyncStatus.PendingDownload, IsSelected = true }], cancellationToken);
                            }

                            continue;
                        }

                        var localFilePath = Path.Combine(account.LocalSyncPath, remoteFile.RelativePath?.TrimStart('/') ?? "");
                        var fileWithLocalPath = new FileMetadata(
                            remoteFile.DriveItemId,
                            accountId,
                            remoteFile.Name ?? string.Empty,
                            remoteFile.RelativePath ?? string.Empty,
                            remoteFile.Size,
                            remoteFile.LastModifiedUtc,
                            localFilePath,
                            IsFolder: false,
                            IsDeleted: false);
                        filesToDownload.Add(fileWithLocalPath);
                    }
                }
                else
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"File not in DB: {remoteFile.RelativePath} - first sync or new file", cancellationToken);
                    if(localFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out FileMetadata? localFile))
                    {
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
                            filesToRecordWithoutTransfer.Add(matchedFile);
                        }
                        else
                        {
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                                $"First sync CONFLICT: {remoteFile.RelativePath} - files differ (TimeDiff={timeDiff:F1}s, SizeMatch={localFile.Size == remoteFile.Size})", cancellationToken);
                            var conflict = SyncConflict.CreateUnresolvedConflict(accountId, remoteFile.RelativePath ?? "", localFile.LastModifiedUtc, remoteFile.LastModifiedUtc, localFile.Size, remoteFile.Size);

                            await _syncConflictRepository.AddAsync(conflict, cancellationToken);
                            conflictCount++;
                            _ = conflictPaths.Add(remoteFile.RelativePath ?? "");
                            if(_currentSessionId is not null)
                            {
                                var operationLog = FileOperationLog.CreateSyncConflictLog(_currentSessionId, accountId, remoteFile.RelativePath ?? "", localFile.LocalPath, remoteFile.DriveItemId,
                                    localFile.LocalHash, localFile.Size, localFile.LastModifiedUtc, remoteFile.LastModifiedUtc);
                                await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                                await _driveItemsRepository.SaveBatchAsync([localFile with { SyncStatus = FileSyncStatus.PendingDownload }], cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        var localFilePath = Path.Combine(account.LocalSyncPath, remoteFile.RelativePath?.TrimStart('/') ?? "");
                        var fileWithLocalPath = new FileMetadata(
                            remoteFile.DriveItemId,
                            accountId,
                            remoteFile.Name ?? string.Empty,
                            remoteFile.RelativePath ?? string.Empty,
                            remoteFile.Size,
                            remoteFile.LastModifiedUtc,
                            localFilePath,
                            IsFolder: false,
                            IsDeleted: false);
                        filesToDownload.Add(fileWithLocalPath);
                        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"New remote file to download: {remoteFile.RelativePath}", cancellationToken);
                    }
                }
            }

            var localPathsSet = allLocalFiles.Select(f => f.RelativePath).ToHashSet();
            List<DriveItemEntity> deletedFromOneDrive = SelectFilesDeletedFromOneDriveButSyncedLocally(folders, remotePathsSet, localPathsSet);

            foreach(DriveItemEntity file in deletedFromOneDrive)
            {
                try
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"File deleted from OneDrive: {file.RelativePath} - deleting local copy at {file.LocalPath}", cancellationToken);
                    if(System.IO.File.Exists(file.LocalPath))
                        System.IO.File.Delete(file.LocalPath);

                    await _driveItemsRepository.DeleteAsync(file.DriveItemId, cancellationToken);
                }
                catch(Exception ex)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Failed to delete local file {file.RelativePath}: {ex.Message}. Continuing with other deletions.", cancellationToken);
                }
            }

            List<FileMetadata> deletedLocally = GetFilesDeletedLocally(allLocalFiles, remotePathsSet, localPathsSet);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Local deletion detection: {deletedLocally.Count} files to delete from OneDrive.", cancellationToken);
            foreach(FileMetadata file in deletedLocally)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                    $"Candidate for remote deletion: Path={file.RelativePath}, Id={file.DriveItemId}, SyncStatus={file.SyncStatus}, ExistsLocally={System.IO.File.Exists(file.LocalPath)}, ExistsRemotely={remotePathsSet.Contains(file.RelativePath)}",
                    cancellationToken);
            }

            foreach(FileMetadata file in deletedLocally)
            {
                try
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Deleting from OneDrive: Path={file.RelativePath}, Id={file.DriveItemId}, SyncStatus={file.SyncStatus}", cancellationToken);
                    await _graphApiClient.DeleteFileAsync(accountId, file.DriveItemId, cancellationToken);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Deleted from OneDrive: Path={file.RelativePath}, Id={file.DriveItemId}", cancellationToken);
                    await _driveItemsRepository.DeleteAsync(file.DriveItemId, cancellationToken);
                }
                catch(Exception ex)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Failed to delete from OneDrive {file.RelativePath}: {ex.Message}, continuing the sync...", cancellationToken);
                }
            }

            var alreadyProcessedDeletions = deletedFromOneDrive.Select(f => f.DriveItemId)
                .Concat(deletedLocally.Select(f => f.DriveItemId))
                .ToHashSet();
            List<DriveItemEntity> filesToDelete = GetFilesToDelete(folders, remotePathsSet, localPathsSet, alreadyProcessedDeletions);

            var uploadPathsSet = filesToUpload.Select(f => f.RelativePath).ToHashSet();
            var deletedPaths = deletedFromOneDrive.Select(f => f.RelativePath).ToHashSet();
            filesToUpload = [.. filesToUpload.Where(f => !deletedPaths.Contains(f.RelativePath) && !conflictPaths.Contains(f.RelativePath))];

            var totalFiles = filesToUpload.Count + filesToDownload.Count;
            var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            var uploadBytes = filesToUpload.Sum(f => f.Size);
            var downloadBytes = filesToDownload.Sum(f => f.Size);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Sync summary: {filesToDownload.Count} to download, {filesToUpload.Count} to upload, {filesToDelete.Count} to delete",
                cancellationToken);

            (filesToDownload, totalFiles, totalBytes, downloadBytes) = await RemoveDuplicatesFromDownloadList(filesToUpload, filesToDownload, totalFiles, totalBytes, downloadBytes, accountId, cancellationToken);

            ReportProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            var completedFiles = 0;
            long completedBytes = 0;

            var maxParallelUploads = Math.Max(1, account.MaxParallelUpDownloads);
            using var uploadSemaphore = new SemaphoreSlim(maxParallelUploads, maxParallelUploads);
            var activeUploads = 0;
            (activeUploads, completedBytes, completedFiles, List<Task>? uploadTasks) = CreateUploadTasks(accountId, folders, filesToUpload, conflictCount, totalFiles, totalBytes,
                uploadBytes, completedFiles, completedBytes, uploadSemaphore, activeUploads, cancellationToken);

            await Task.WhenAll(uploadTasks);

            ResetTrackingDetails(completedBytes);

            var maxParallelDownloads = Math.Max(1, account.MaxParallelUpDownloads);
            using var downloadSemaphore = new SemaphoreSlim(maxParallelDownloads, maxParallelDownloads);
            var activeDownloads = 0;
            (activeDownloads, completedBytes, completedFiles, List<Task>? downloadTasks) = CreateDownloadTasks(accountId, folders, filesToDownload, conflictCount, totalFiles, totalBytes,
                uploadBytes, downloadBytes, completedFiles, completedBytes, downloadSemaphore, activeDownloads, cancellationToken);

            await Task.WhenAll(downloadTasks);

            await DeleteDeletedFilesFromDatabase(filesToDelete, cancellationToken);

            ReportProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes, completedBytes, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Sync completed: {totalFiles} files, {completedBytes} bytes", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", accountId, cancellationToken);

            await FinalizeSyncSessionAsync(_currentSessionId, filesToUpload.Count, filesToDownload.Count, filesToDelete.Count, conflictCount, completedBytes, account, cancellationToken);
        }
        catch(OperationCanceledException)
        {
            await HandleSyncCancelledAsync(_currentSessionId, cancellationToken);
            ReportProgress(accountId, SyncStatus.Paused);
            throw;
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", accountId, $"Sync failed: {ex.Message}", ex, cancellationToken);
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

    /// <inheritdoc />
    public Task StopSyncAsync()
    {
        _syncCancellation?.Cancel();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default)
        => await _syncConflictRepository.GetUnresolvedByAccountIdAsync(accountId, cancellationToken);

    private async Task DeleteDeletedFilesFromDatabase(List<DriveItemEntity> filesToDelete, CancellationToken cancellationToken)
    {
        foreach(DriveItemEntity fileToDelete in filesToDelete)
            await _driveItemsRepository.DeleteAsync(fileToDelete.DriveItemId, cancellationToken);
    }

    private (int activeDownloads, long completedBytes, int completedFiles, List<Task> downloadTasks) CreateDownloadTasks(string accountId, IReadOnlyList<DriveItemEntity> existingItems,
        List<FileMetadata> filesToDownload, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, long downloadBytes, int completedFiles, long completedBytes,
        SemaphoreSlim downloadSemaphore, int activeDownloads, CancellationToken cancellationToken)
    {
        var batchSize = 50;
        var batch = new List<FileMetadata>(batchSize);
        var downloadTasks = filesToDownload.Select(async file =>
        {
            await downloadSemaphore.WaitAsync(_syncCancellation!.Token);
            _ = Interlocked.Increment(ref activeDownloads);

            try
            {
                _syncCancellation!.Token.ThrowIfCancellationRequested();

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Starting download: {file.Name} (ID: {file.DriveItemId}) to {file.LocalPath}", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.DownloadFile", accountId, $"Starting download: {file.Name} (ID: {file.DriveItemId}) to {file.LocalPath}", _syncCancellation!.Token);

                var directory = Path.GetDirectoryName(file.LocalPath);
                if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    await DebugLog.InfoAsync("SyncEngine.DownloadFile", accountId, $"Creating directory: {directory}", _syncCancellation!.Token);
                    _ = Directory.CreateDirectory(directory);
                }

                if(_currentSessionId is not null)
                {
                    DriveItemEntity? existingFile = existingItems.FirstOrDefault(ie => ie.RelativePath == file.RelativePath && (ie.SyncStatus != FileSyncStatus.Failed || ie.SyncStatus == FileSyncStatus.PendingUpload));
                    var isExistingFile = existingFile is not null;
                    var reason = isExistingFile ? "Remote file changed" : "New remote file";
                    var operationLog = FileOperationLog.CreateDownloadLog(
                        _currentSessionId, accountId, file.RelativePath, file.LocalPath, file.DriveItemId,
                        existingFile?.LocalHash, file.Size, file.LastModifiedUtc, reason);
                    await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                    await _driveItemsRepository.SaveBatchAsync([file with { SyncStatus = FileSyncStatus.PendingDownload }], cancellationToken);
                }

                await _graphApiClient.DownloadFileAsync(accountId, file.DriveItemId, file.LocalPath, _syncCancellation!.Token);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Download complete: {file.Name}, computing hash...", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.DownloadFile", accountId, $"Download complete: {file.Name}, computing hash...", _syncCancellation!.Token);

                var downloadedHash = await _localFileScanner.ComputeFileHashAsync(file.LocalPath, _syncCancellation!.Token);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Hash computed for {file.Name}: {downloadedHash}", cancellationToken);

                FileMetadata downloadedFile = file with { SyncStatus = FileSyncStatus.Synced, LastSyncDirection = SyncDirection.Download, LocalHash = downloadedHash };

                batch.Add(downloadedFile);
                if(batch.Count >= batchSize)
                {
                    await _driveItemsRepository.SaveBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Successfully synced: {file.Name}", cancellationToken);

                _ = Interlocked.Increment(ref completedFiles);
                _ = Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                var finalActiveDownloads = Interlocked.CompareExchange(ref activeDownloads, 0, 0);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, finalActiveDownloads, conflictsDetected: conflictCount,
                    phaseTotalBytes: uploadBytes + downloadBytes);
            }
            catch(Exception ex)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"ERROR downloading {file.Name}: {ex.GetType().Name} - {ex.Message}", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Stack trace: {ex.StackTrace}", cancellationToken);
                await DebugLog.ErrorAsync("SyncEngine.DownloadFile", accountId, $"ERROR downloading {file.Name}: {ex.Message}", ex, _syncCancellation!.Token);

                FileMetadata failedFile = file with { SyncStatus = FileSyncStatus.Failed };
                batch.Add(failedFile);
                if(batch.Count >= batchSize)
                {
                    await _driveItemsRepository.SaveBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }

                _ = Interlocked.Increment(ref completedFiles);
                _ = Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes + downloadBytes);
            }
            finally
            {
                _ = Interlocked.Decrement(ref activeDownloads);
                _ = downloadSemaphore.Release();
            }
        }).ToList();
        // Save any remaining files in the batch after all downloads complete
        if(batch.Count > 0)
        {
            _driveItemsRepository.SaveBatchAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
            batch.Clear();
        }

        return (activeDownloads, completedBytes, completedFiles, downloadTasks);
    }

    private (int activeUploads, long completedBytes, int completedFiles, List<Task> uploadTasks) CreateUploadTasks(string accountId, IReadOnlyList<DriveItemEntity> existingItems,
        List<FileMetadata> filesToUpload, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, int completedFiles, long completedBytes, SemaphoreSlim uploadSemaphore,
        int activeUploads, CancellationToken cancellationToken)
    {
        var batchSize = 50;
        var batch = new List<FileMetadata>(batchSize);
        var uploadTasks = filesToUpload.Select(async file =>
        {
            await uploadSemaphore.WaitAsync(_syncCancellation!.Token);
            _ = Interlocked.Increment(ref activeUploads);

            try
            {
                _syncCancellation!.Token.ThrowIfCancellationRequested();

                DriveItemEntity? existingFile = existingItems.FirstOrDefault(ie => ie.RelativePath == file.RelativePath && (ie.SyncStatus != FileSyncStatus.Failed || ie.SyncStatus == FileSyncStatus.PendingUpload));
                var isExistingFile = existingFile is not null;

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Uploading {file.Name}: Path={file.RelativePath}, IsExisting={isExistingFile}, LocalPath={file.LocalPath}", cancellationToken);

                if(!isExistingFile)
                {
                    FileMetadata pendingFile = file with { SyncStatus = FileSyncStatus.PendingUpload };
                    await _driveItemsRepository.AddAsync(pendingFile, cancellationToken);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Added pending upload record to database: {file.Name}", cancellationToken);
                }

                if(_currentSessionId is not null)
                {
                    var reason = isExistingFile ? "File changed locally" : "New file";
                    var operationLog = FileOperationLog.CreateUploadLog(_currentSessionId, accountId, file.RelativePath, file.LocalPath, existingFile?.DriveItemId,
                        existingFile?.LocalHash, file.Size, file.LastModifiedUtc, reason);

                    if(existingFile is not null)
                        await _driveItemsRepository.SaveBatchAsync([new FileMetadata(existingFile.DriveItemId, existingFile.AccountId, existingFile.Name ?? "", existingFile.RelativePath, existingFile.Size, existingFile.LastModifiedUtc, existingFile.LocalPath ?? "", existingFile.IsFolder, existingFile.IsDeleted, existingFile.IsSelected??false, existingFile.RemoteHash, existingFile.CTag, existingFile.ETag, existingFile.LocalHash, FileSyncStatus.PendingDownload, SyncDirection.Download) ?? file], cancellationToken);
                }

                var baseCompletedBytes = Interlocked.Read(ref completedBytes);
                var currentActiveUploads = Interlocked.CompareExchange(ref activeUploads, 0, 0);
                var uploadProgress = new Progress<long>(bytesUploaded =>
                {
                    var currentCompletedBytes = baseCompletedBytes + bytesUploaded;
                    var currentCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                    ReportProgress(accountId, SyncStatus.Running, totalFiles, currentCompleted, totalBytes, currentCompletedBytes, filesUploading: currentActiveUploads,
                        conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes);
                });

                DriveItem uploadedItem = await _graphApiClient.UploadFileAsync(
                    accountId,
                    file.LocalPath,
                    file.RelativePath,
                    uploadProgress,
                    _syncCancellation!.Token);

                if(uploadedItem.LastModifiedDateTime.HasValue && System.IO.File.Exists(file.LocalPath))
                {
                    System.IO.File.SetLastWriteTimeUtc(file.LocalPath, uploadedItem.LastModifiedDateTime.Value.UtcDateTime);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                        $"Synchronized local timestamp to OneDrive: {file.Name}, OldTime={file.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, NewTime={uploadedItem.LastModifiedDateTime.Value.UtcDateTime:yyyy-MM-dd HH:mm:ss}",
                        cancellationToken);
                }

                DateTimeOffset oneDriveTimestamp = uploadedItem.LastModifiedDateTime ?? file.LastModifiedUtc;

                FileMetadata uploadedFile = isExistingFile && existingFile is not null
                    ? new FileMetadata(existingFile.DriveItemId, existingFile.AccountId, existingFile.Name ?? "",  existingFile.RelativePath, existingFile.Size, existingFile.LastModifiedUtc, existingFile.LocalPath ?? "", existingFile.IsFolder, existingFile.IsDeleted, existingFile.IsSelected??false, existingFile.RemoteHash, existingFile.CTag, existingFile.ETag, existingFile.LocalHash, FileSyncStatus.PendingDownload, SyncDirection.Download)
                    : file with
                    {
                        DriveItemId = uploadedItem.Id ?? throw new InvalidOperationException($"Upload succeeded but no ID returned for {file.Name}"),
                        CTag = uploadedItem.CTag,
                        ETag = uploadedItem.ETag,
                        LastModifiedUtc = oneDriveTimestamp,
                        SyncStatus = FileSyncStatus.Synced,
                        LastSyncDirection = SyncDirection.Upload
                    };

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Upload successful: {file.Name}, OneDrive ID={uploadedFile.DriveItemId}, CTag={uploadedFile.CTag}", cancellationToken);

                batch.Add(uploadedFile);
                if(batch.Count >= batchSize)
                {
                    await _driveItemsRepository.SaveBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }

                _ = Interlocked.Increment(ref completedFiles);
                _ = Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                var finalActiveUploads = Interlocked.CompareExchange(ref activeUploads, 0, 0);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, filesUploading: finalActiveUploads, conflictsDetected: conflictCount,
                    phaseTotalBytes: uploadBytes);
            }
            catch(Exception ex)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Upload failed for {file.Name}: {ex.Message}", cancellationToken);

                FileMetadata failedFile = file with { SyncStatus = FileSyncStatus.Failed };

                FileMetadata? existingDbFile = !string.IsNullOrEmpty(failedFile.DriveItemId)
                    ? await _driveItemsRepository.GetByIdAsync(failedFile.DriveItemId, cancellationToken)
                    : await _driveItemsRepository.GetByPathAsync(accountId, failedFile.RelativePath, cancellationToken);

                if(existingDbFile is not null)
                    batch.Add(failedFile);
                else
                    await _driveItemsRepository.AddAsync(failedFile, cancellationToken);

                if(batch.Count >= batchSize)
                {
                    await _driveItemsRepository.SaveBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }

                _ = Interlocked.Increment(ref completedFiles);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes);
            }
            finally
            {
                _ = Interlocked.Decrement(ref activeUploads);
                _ = uploadSemaphore.Release();
            }
        }).ToList();
        // Save any remaining files in the batch after all uploads complete
        if(batch.Count > 0)
        {
            _driveItemsRepository.SaveBatchAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
            batch.Clear();
        }

        return (activeUploads, completedBytes, completedFiles, uploadTasks);
    }

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

    private static List<DriveItemEntity> GetFilesToDelete(IReadOnlyList<DriveItemEntity> existingFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet, HashSet<string> alreadyProcessedDeletions)
        => localPathsSet.Count == 0
            ? []
            : [
                .. existingFiles
                .Where(f => !remotePathsSet.Contains(f.RelativePath) &&
                            !localPathsSet.Contains(f.RelativePath) &&
                            !string.IsNullOrWhiteSpace(f.DriveItemId) &&
                            !alreadyProcessedDeletions.Contains(f.DriveItemId))
                .Where(f => f.DriveItemId is not null)
            ];

    private static List<FileMetadata> GetFilesDeletedLocally(List<FileMetadata> allLocalFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet)
        => [
            .. allLocalFiles
                .Where(f => !localPathsSet.Contains(f.RelativePath) &&
                            (remotePathsSet.Contains(f.RelativePath) || f.SyncStatus == FileSyncStatus.Synced) &&
                            !string.IsNullOrEmpty(f.DriveItemId))
        ];

    private static List<DriveItemEntity> SelectFilesDeletedFromOneDriveButSyncedLocally(IReadOnlyList<DriveItemEntity> existingFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet)
        => [
            .. existingFiles
                .Where(f => !remotePathsSet.Contains(f.RelativePath) &&
                            localPathsSet.Contains(f.RelativePath) &&
                            f.SyncStatus == FileSyncStatus.Synced)
        ];

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
