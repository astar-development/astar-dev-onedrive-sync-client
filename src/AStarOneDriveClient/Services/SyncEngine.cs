using System.Reactive.Subjects;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services.OneDriveServices;

namespace AStarOneDriveClient.Services;

/// <summary>
/// Service for synchronizing files between local storage and OneDrive.
/// </summary>
/// <remarks>
/// Supports bidirectional sync with conflict detection and resolution.
/// Uses LastWriteWins strategy: when both local and remote files change, the newer timestamp wins.
/// </remarks>
public sealed partial class SyncEngine : ISyncEngine, IDisposable
{
    private const double AllowedTimeDifference = 60.0;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly IRemoteChangeDetector _remoteChangeDetector;
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly ISyncConfigurationRepository _syncConfigurationRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IGraphApiClient _graphApiClient;
    private readonly ISyncConflictRepository _syncConflictRepository;
    private readonly ISyncSessionLogRepository _syncSessionLogRepository;
    private readonly IFileOperationLogRepository _fileOperationLogRepository;
    private readonly BehaviorSubject<SyncState> _progressSubject;
    private CancellationTokenSource? _syncCancellation;
    private string? _currentSessionId;
    private int _syncInProgress;
    private DateTime _lastProgressUpdate = DateTime.UtcNow;
    private long _lastCompletedBytes;
    private readonly List<(DateTime Timestamp, long Bytes)> _transferHistory = [];

    public SyncEngine(
        ILocalFileScanner localFileScanner,
        IRemoteChangeDetector remoteChangeDetector,
        IFileMetadataRepository fileMetadataRepository,
        ISyncConfigurationRepository syncConfigurationRepository,
        IAccountRepository accountRepository,
        IGraphApiClient graphApiClient,
        ISyncConflictRepository syncConflictRepository,
        ISyncSessionLogRepository syncSessionLogRepository,
        IFileOperationLogRepository fileOperationLogRepository)
    {
        _localFileScanner = localFileScanner ?? throw new ArgumentNullException(nameof(localFileScanner));
        _remoteChangeDetector = remoteChangeDetector ?? throw new ArgumentNullException(nameof(remoteChangeDetector));
        _fileMetadataRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
        _syncConfigurationRepository = syncConfigurationRepository ?? throw new ArgumentNullException(nameof(syncConfigurationRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _syncConflictRepository = syncConflictRepository ?? throw new ArgumentNullException(nameof(syncConflictRepository));
        _syncSessionLogRepository = syncSessionLogRepository ?? throw new ArgumentNullException(nameof(syncSessionLogRepository));
        _fileOperationLogRepository = fileOperationLogRepository ?? throw new ArgumentNullException(nameof(fileOperationLogRepository));

        var initialState = SyncState.CreateInitial(string.Empty);

        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }

    /// <inheritdoc/>
    public IObservable<SyncState> Progress => _progressSubject;

    /// <inheritdoc/>
    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        DebugLogContext.SetAccountId(accountId);

        if (SyncIsAlreadyRunning())
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Sync already in progress for account {accountId}, ignoring duplicate request", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", cancellationToken);
            return;
        }

        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            ResetBeforeRunning();

            await DebugLog.EntryAsync("SyncEngine.StartSyncAsync", cancellationToken);

            ReportProgress(accountId, SyncStatus.Running, 0, 0, 0, 0);

            var selectedFolders = await _syncConfigurationRepository.GetSelectedFoldersAsync(accountId, cancellationToken);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Starting sync with {selectedFolders.Count} selected folders: {string.Join(", ", selectedFolders)}", cancellationToken);

            if (selectedFolders.Count == 0)
            {
                ReportProgress(accountId, SyncStatus.Idle, 0, 0, 0, 0);
                return;
            }

            var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
            {
                ReportProgress(accountId, SyncStatus.Failed, 0, 0, 0, 0);
                return;
            }

            if (account.EnableDetailedSyncLogging)
            {
                var sessionLog = SyncSessionLog.CreateInitialRunning(accountId);
                await _syncSessionLogRepository.AddAsync(sessionLog, cancellationToken);
                _currentSessionId = sessionLog.Id;
            }
            else
            {
                _currentSessionId = null;
            }

            var allLocalFiles = await GetAllLocalFiles(accountId, selectedFolders, account);
            var existingFiles = await _fileMetadataRepository.GetByAccountIdAsync(accountId, cancellationToken);
            var existingFilesDict = existingFiles.ToDictionary(f => f.Path ?? "", f => f);

            // Detect remote changes in selected folders FIRST (before deciding what to upload)
            var allRemoteFiles = new List<FileMetadata>();
            foreach (var folder in selectedFolders)
            {
                if (string.IsNullOrEmpty(folder))
                    continue;
                var displayFolder = FormatScanningFolderForDisplay(folder);
                ReportProgress(accountId, SyncStatus.Running, 0, 0, 0, 0, currentScanningFolder: displayFolder);

                var (remoteFiles, _) = await _remoteChangeDetector.DetectChangesAsync(accountId, folder, previousDeltaLink: null, _syncCancellation!.Token);
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Folder '{folder}' returned {remoteFiles?.Count ?? 0} remote files", cancellationToken);
                if (remoteFiles?.Count > 0)
                {
                    allRemoteFiles.AddRange(remoteFiles);
                }
            }

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Total remote files before deduplication: {allRemoteFiles.Count}", cancellationToken);

            allRemoteFiles = [.. allRemoteFiles
                .GroupBy(f => f.Path ?? "")
                .Select(g => g.First())];

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Total remote files after deduplication: {allRemoteFiles.Count}", cancellationToken);
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Remote file paths: {string.Join(", ", allRemoteFiles.Select(f => f.Path))}", cancellationToken);

            var remoteFilesDict = allRemoteFiles.ToDictionary(f => f.Path ?? "", f => f);
            var localFilesDict = allLocalFiles.ToDictionary(f => f.Path ?? "", f => f);

            var filesToUpload = new List<FileMetadata>();
            foreach (var localFile in allLocalFiles)
            {
                if (existingFilesDict.TryGetValue(localFile.Path, out var existingFile))
                {
                    if (existingFile.SyncStatus is FileSyncStatus.PendingUpload or FileSyncStatus.Failed)
                    {
                        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"File needs upload (status={existingFile.SyncStatus}): {localFile.Name}", cancellationToken);
                        var fileToUpload = existingFile with
                        {
                            LocalPath = localFile.LocalPath,
                            LocalHash = localFile.LocalHash,
                            Size = localFile.Size,
                            LastModifiedUtc = localFile.LastModifiedUtc
                        };
                        filesToUpload.Add(fileToUpload);
                    }
                    else
                    {
                        var bothHaveHashes = !string.IsNullOrEmpty(existingFile.LocalHash) && !string.IsNullOrEmpty(localFile.LocalHash);

                        bool hasChanged;
                        if (bothHaveHashes)
                        {
                            hasChanged = existingFile.LocalHash != localFile.LocalHash;
                            if (hasChanged)
                            {
                                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"File marked as changed: {localFile.Name} - Hash changed (DB: {existingFile.LocalHash}, Local: {localFile.LocalHash})", cancellationToken);
                            }
                        }
                        else
                        {
                            hasChanged = existingFile.Size != localFile.Size;
                            if (hasChanged)
                            {
                                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"File marked as changed: {localFile.Name} - Size changed (DB: {existingFile.Size}, Local: {localFile.Size})", cancellationToken);
                            }
                        }

                        if (hasChanged)
                        {
                            filesToUpload.Add(localFile);
                        }
                    }
                }
                else if (!remoteFilesDict.ContainsKey(localFile.Path))
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"New local file to upload: {localFile.Name}", cancellationToken);
                    filesToUpload.Add(localFile);
                }
            }

            var filesToDownload = new List<FileMetadata>();
            var remotePathsSet = allRemoteFiles.Select(f => f.Path).ToHashSet();
            var conflictCount = 0;
            var conflictPaths = new HashSet<string>();
            var filesToRecordWithoutTransfer = new List<FileMetadata>();

            foreach (var remoteFile in allRemoteFiles)
            {
                if (existingFilesDict.TryGetValue(remoteFile.Path, out var existingFile))
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Found file in DB: {remoteFile.Path}, DB Status={existingFile.SyncStatus}", cancellationToken);
                    var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                    var remoteHasChanged = (!string.IsNullOrWhiteSpace(existingFile.CTag) ||
                                         timeDiff > 3600.0 ||
                                         existingFile.Size != remoteFile.Size) && (existingFile.CTag != remoteFile.CTag);

                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Remote file check: {remoteFile.Path} - DB CTag={existingFile.CTag}, Remote CTag={remoteFile.CTag}, DB Time={existingFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Diff={timeDiff:F1}s, DB Size={existingFile.Size}, Remote Size={remoteFile.Size}, RemoteHasChanged={remoteHasChanged}", cancellationToken);

                    if (remoteHasChanged)
                    {
                        var localFileHasChanged = false;

                        if (localFilesDict.TryGetValue(remoteFile.Path, out var localFile))
                        {
                            var localTimeDiff = Math.Abs((existingFile.LastModifiedUtc - localFile.LastModifiedUtc).TotalSeconds);
                            localFileHasChanged = localTimeDiff > 1.0 || existingFile.Size != localFile.Size;
                        }

                        if (localFileHasChanged)
                        {
                            var localFileFromDict = localFilesDict[remoteFile.Path];
                            var conflict = SyncConflict.CreateUnresolvedConflict(accountId, remoteFile.Path, localFileFromDict.LastModifiedUtc, remoteFile.LastModifiedUtc, localFileFromDict.Size, remoteFile.Size);

                            var existingConflict = await _syncConflictRepository.GetByFilePathAsync(accountId, remoteFile.Path, cancellationToken);
                            if (existingConflict is null)
                            {
                                await _syncConflictRepository.AddAsync(conflict, cancellationToken);
                                conflictCount++;
                            }
                            else
                            {
                                conflictCount++;
                            }

                            conflictPaths.Add(remoteFile.Path);
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"CONFLICT detected for {remoteFile.Path}: local and remote both changed", cancellationToken);

                            if (_currentSessionId is not null)
                            {
                                var operationLog = FileOperationLog.CreateSyncConflictLog(_currentSessionId, accountId, remoteFile.Path, localFileFromDict.LocalPath, remoteFile.Id,
                                    localFileFromDict.LocalHash, localFileFromDict.Size, localFileFromDict.LastModifiedUtc, remoteFile.LastModifiedUtc);
                                await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                            }

                            continue;
                        }

                        var localFilePath = Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                        var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                        filesToDownload.Add(fileWithLocalPath);
                    }
                }
                else
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"File NOT in DB: {remoteFile.Path} - first sync or new file", cancellationToken);
                    if (localFilesDict.TryGetValue(remoteFile.Path, out var localFile))
                    {
                        var timeDiff = Math.Abs((localFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                        var filesMatch = localFile.Size == remoteFile.Size && timeDiff <= AllowedTimeDifference;

                        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"First sync compare: {remoteFile.Path} - Local: Size={localFile.Size}, Time={localFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote: Size={remoteFile.Size}, Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, TimeDiff={timeDiff:F1}s, Match={filesMatch}", cancellationToken);

                        if (filesMatch)
                        {
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"File exists both places and matches: {remoteFile.Path} - recording in DB", cancellationToken);
                            var matchedFile = localFile with
                            {
                                Id = remoteFile.Id,
                                CTag = remoteFile.CTag,
                                ETag = remoteFile.ETag,
                                SyncStatus = FileSyncStatus.Synced,
                                LastSyncDirection = null
                            };
                            filesToRecordWithoutTransfer.Add(matchedFile);
                        }
                        else
                        {
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"First sync CONFLICT: {remoteFile.Path} - files differ (TimeDiff={timeDiff:F1}s, SizeMatch={localFile.Size == remoteFile.Size})", cancellationToken);
                            var conflict = SyncConflict.CreateUnresolvedConflict(accountId, remoteFile.Path, localFile.LastModifiedUtc, remoteFile.LastModifiedUtc, localFile.Size, remoteFile.Size);

                            await _syncConflictRepository.AddAsync(conflict, cancellationToken);
                            conflictCount++;
                            conflictPaths.Add(remoteFile.Path);

                            if (_currentSessionId is not null)
                            {
                                var operationLog = FileOperationLog.CreateSyncConflictLog(_currentSessionId, accountId, remoteFile.Path, localFile.LocalPath, remoteFile.Id,
                                    localFile.LocalHash, localFile.Size, localFile.LastModifiedUtc, remoteFile.LastModifiedUtc);
                                await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        var localFilePath = Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                        var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                        filesToDownload.Add(fileWithLocalPath);
                        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"New remote file to download: {remoteFile.Path}", cancellationToken);
                    }
                }
            }

            var localPathsSet = allLocalFiles.Select(f => f.Path).ToHashSet();
            var deletedFromOneDrive = SelectFilesDeletedFromOneDriveButSyncedLocally(existingFiles, remotePathsSet, localPathsSet);

            foreach (var file in deletedFromOneDrive)
            {
                try
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"File deleted from OneDrive: {file.Path} - deleting local copy at {file.LocalPath}", cancellationToken);
                    if (File.Exists(file.LocalPath))
                    {
                        File.Delete(file.LocalPath);
                    }

                    await _fileMetadataRepository.DeleteAsync(file.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Failed to delete local file {file.Path}: {ex.Message}. Continuing with other deletions.", cancellationToken);
                }
            }
            var deletedLocally = GetFilesDeletedLocally(allLocalFiles, remotePathsSet, localPathsSet);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Local deletion detection: {deletedLocally.Count} files to delete from OneDrive.", cancellationToken);
            foreach (var file in deletedLocally)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Candidate for remote deletion: Path={file.Path}, Id={file.Id}, SyncStatus={file.SyncStatus}, ExistsLocally={File.Exists(file.LocalPath)}, ExistsRemotely={remotePathsSet.Contains(file.Path)}", cancellationToken);
            }

            foreach (var file in deletedLocally)
            {
                try
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Deleting from OneDrive: Path={file.Path}, Id={file.Id}, SyncStatus={file.SyncStatus}", cancellationToken);
                    await _graphApiClient.DeleteFileAsync(accountId, file.Id, cancellationToken);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Deleted from OneDrive: Path={file.Path}, Id={file.Id}", cancellationToken);
                    await _fileMetadataRepository.DeleteAsync(file.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Failed to delete from OneDrive {file.Path}: {ex.Message}, continuing the sync...", cancellationToken);
                }
            }

            var alreadyProcessedDeletions = deletedFromOneDrive.Select(f => f.Id)
                .Concat(deletedLocally.Select(f => f.Id))
                .ToHashSet();
            var filesToDelete = GetFilesToDelete(existingFiles, remotePathsSet, localPathsSet, alreadyProcessedDeletions);

            var uploadPathsSet = filesToUpload.Select(f => f.Path).ToHashSet();
            var deletedPaths = deletedFromOneDrive.Select(f => f.Path).ToHashSet();
            filesToUpload = [.. filesToUpload.Where(f => !deletedPaths.Contains(f.Path) && !conflictPaths.Contains(f.Path))];

            var totalFiles = filesToUpload.Count + filesToDownload.Count;
            var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            var uploadBytes = filesToUpload.Sum(f => f.Size);
            var downloadBytes = filesToDownload.Sum(f => f.Size);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Sync summary: {filesToDownload.Count} to download, {filesToUpload.Count} to upload, {filesToDelete.Count} to delete", cancellationToken);

            (filesToDownload, totalFiles, totalBytes, downloadBytes) = await RemoveDuplicatesFromDownloadList(filesToUpload, filesToDownload, totalFiles, totalBytes, downloadBytes, cancellationToken);

            ReportProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes, 0, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            var completedFiles = 0;
            long completedBytes = 0;

            var maxParallelUploads = Math.Max(1, account.MaxParallelUpDownloads);
            using var uploadSemaphore = new SemaphoreSlim(maxParallelUploads, maxParallelUploads);
            var activeUploads = 0;
            var uploadTasks = CreateUploadTasks(accountId, existingFilesDict, filesToUpload, conflictCount, totalFiles, totalBytes, uploadBytes, completedFiles, completedBytes, uploadSemaphore, activeUploads, cancellationToken);

            await Task.WhenAll(uploadTasks);

            ResetTrackingDetails(completedBytes);

            var maxParallelDownloads = Math.Max(1, account.MaxParallelUpDownloads);
            using var downloadSemaphore = new SemaphoreSlim(maxParallelDownloads, maxParallelDownloads);
            var activeDownloads = 0;
            var downloadTasks = GenerateDownloadTasks(accountId, existingFilesDict, filesToDownload, conflictCount, totalFiles, totalBytes, uploadBytes, downloadBytes, completedFiles, completedBytes, downloadSemaphore, activeDownloads, cancellationToken);

            await Task.WhenAll(downloadTasks);

            await DeleteDeletedFilesFromDatabase(filesToDelete, cancellationToken);

            ReportProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes, completedBytes, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Sync completed: {completedFiles}/{totalFiles} files, {completedBytes} bytes", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", cancellationToken);

            await FinalizeSyncSessionAsync(_currentSessionId, filesToUpload.Count, filesToDownload.Count, filesToDelete.Count, conflictCount, completedBytes, account, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await HandleSyncCancelledAsync(_currentSessionId, cancellationToken);
            ReportProgress(accountId, SyncStatus.Paused, 0, 0, 0, 0);
            throw;
        }
        catch (Exception ex)
        {
            await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", $"Sync failed: {ex.Message}", ex, cancellationToken);
            await HandleSyncFailedAsync(_currentSessionId, cancellationToken);
            ReportProgress(accountId, SyncStatus.Failed, 0, 0, 0, 0);
            throw;
        }
        finally
        {
            DebugLogContext.Clear();
            Interlocked.Exchange(ref _syncInProgress, 0);
        }
    }

    private async Task DeleteDeletedFilesFromDatabase(List<FileMetadata> filesToDelete, CancellationToken cancellationToken)
    {
        foreach (var fileToDelete in filesToDelete)
        {
            await _fileMetadataRepository.DeleteAsync(fileToDelete.Id, cancellationToken);
        }
    }

    private List<Task> GenerateDownloadTasks(string accountId, Dictionary<string, FileMetadata> existingFilesDict, List<FileMetadata> filesToDownload, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, long downloadBytes, int completedFiles, long completedBytes, SemaphoreSlim downloadSemaphore, int activeDownloads, CancellationToken cancellationToken)
    {
        var downloadTasks = filesToDownload.Select(async file =>
        {
            await downloadSemaphore.WaitAsync(_syncCancellation!.Token);
            Interlocked.Increment(ref activeDownloads);

            try
            {
                _syncCancellation!.Token.ThrowIfCancellationRequested();

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Starting download: {file.Name} (ID: {file.Id}) to {file.LocalPath}", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.DownloadFile", $"Starting download: {file.Name} (ID: {file.Id}) to {file.LocalPath}", _syncCancellation!.Token);

                var directory = Path.GetDirectoryName(file.LocalPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    await DebugLog.InfoAsync("SyncEngine.DownloadFile", $"Creating directory: {directory}", _syncCancellation!.Token);
                    Directory.CreateDirectory(directory);
                }

                if (_currentSessionId is not null)
                {
                    var existingLocal = existingFilesDict.TryGetValue(file.Path, out var existingFile);
                    var reason = existingLocal ? "Remote file changed" : "New remote file";
                    var operationLog = new FileOperationLog(
                        Id: Guid.CreateVersion7().ToString(),
                        SyncSessionId: _currentSessionId,
                        AccountId: accountId,
                        Timestamp: DateTime.UtcNow,
                        Operation: FileOperation.Download,
                        FilePath: file.Path,
                        LocalPath: file.LocalPath,
                        OneDriveId: file.Id,
                        FileSize: file.Size,
                        LocalHash: existingFile?.LocalHash,
                        RemoteHash: null,
                        LastModifiedUtc: file.LastModifiedUtc,
                        Reason: reason);
                    await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                }

                await _graphApiClient.DownloadFileAsync(accountId, file.Id, file.LocalPath, _syncCancellation!.Token);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Download complete: {file.Name}, computing hash...", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.DownloadFile", $"Download complete: {file.Name}, computing hash...", _syncCancellation!.Token);

                var downloadedHash = await _localFileScanner.ComputeFileHashAsync(file.LocalPath, _syncCancellation!.Token);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Hash computed for {file.Name}: {downloadedHash}", cancellationToken);

                var downloadedFile = file with
                {
                    SyncStatus = FileSyncStatus.Synced,
                    LastSyncDirection = SyncDirection.Download,
                    LocalHash = downloadedHash
                };

                FileMetadata? existingRecord = null;
                if (!string.IsNullOrEmpty(downloadedFile.Id))
                {
                    existingRecord = await _fileMetadataRepository.GetByIdAsync(downloadedFile.Id, cancellationToken);
                }

                existingRecord ??= await _fileMetadataRepository.GetByPathAsync(accountId, downloadedFile.Path, cancellationToken);

                await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Saving {file.Name}: ExistingRecord={(existingRecord is not null ? "Found" : "NotFound")}, ID={downloadedFile.Id}, Path={downloadedFile.Path}", _syncCancellation!.Token);

                try
                {
                    if (existingRecord is not null)
                    {
                        await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Updating existing record: ExistingID={existingRecord.Id}, NewID={downloadedFile.Id}", _syncCancellation!.Token);
                        await _fileMetadataRepository.UpdateAsync(downloadedFile, cancellationToken);
                    }
                    else
                    {
                        await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Adding new record: ID={downloadedFile.Id}", _syncCancellation!.Token);
                        await _fileMetadataRepository.AddAsync(downloadedFile, cancellationToken);
                    }

                    await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Successfully saved {file.Name} to database", _syncCancellation!.Token);
                }
                catch (Exception dbEx)
                {
                    await DebugLog.ErrorAsync("SyncEngine.SaveFileMetadata", $"FAILED to save {file.Name} to database: {dbEx.Message}", dbEx, _syncCancellation!.Token);
                    throw;
                }

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Successfully synced: {file.Name}", cancellationToken);

                Interlocked.Increment(ref completedFiles);
                Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                var finalActiveDownloads = Interlocked.CompareExchange(ref activeDownloads, 0, 0);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, filesDownloading: finalActiveDownloads, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes + downloadBytes);
            }
            catch (Exception ex)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"ERROR downloading {file.Name}: {ex.GetType().Name} - {ex.Message}", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Stack trace: {ex.StackTrace}", cancellationToken);
                await DebugLog.ErrorAsync("SyncEngine.DownloadFile", $"ERROR downloading {file.Name}: {ex.Message}", ex, _syncCancellation!.Token);

                var failedFile = file with { SyncStatus = FileSyncStatus.Failed };

                var existingDbFile = !string.IsNullOrEmpty(failedFile.Id)
                    ? await _fileMetadataRepository.GetByIdAsync(failedFile.Id, cancellationToken)
                    : await _fileMetadataRepository.GetByPathAsync(accountId, failedFile.Path, cancellationToken);

                if (existingDbFile is not null)
                {
                    await _fileMetadataRepository.UpdateAsync(failedFile, cancellationToken);
                }
                else
                {
                    await _fileMetadataRepository.AddAsync(failedFile, cancellationToken);
                }

                Interlocked.Increment(ref completedFiles);
                Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes + downloadBytes);
            }
            finally
            {
                Interlocked.Decrement(ref activeDownloads);
                downloadSemaphore.Release();
            }
        }).ToList();
        return downloadTasks;
    }

    private void ResetTrackingDetails(long completedBytes)
    {
        _transferHistory.Clear();
        _lastProgressUpdate = DateTime.UtcNow;
        _lastCompletedBytes = completedBytes;
    }

    private List<Task> CreateUploadTasks(string accountId, Dictionary<string, FileMetadata> existingFilesDict, List<FileMetadata> filesToUpload, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, int completedFiles, long completedBytes, SemaphoreSlim uploadSemaphore, int activeUploads, CancellationToken cancellationToken)
    {
        var uploadTasks = filesToUpload.Select(async file =>
        {
            await uploadSemaphore.WaitAsync(_syncCancellation!.Token);
            Interlocked.Increment(ref activeUploads);

            try
            {
                _syncCancellation!.Token.ThrowIfCancellationRequested();

                var isExistingFile = existingFilesDict.TryGetValue(file.Path, out var existingFile) &&
                    (!string.IsNullOrEmpty(existingFile.Id) ||
                     existingFile.SyncStatus == FileSyncStatus.PendingUpload ||
                     existingFile.SyncStatus == FileSyncStatus.Failed);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Uploading {file.Name}: Path={file.Path}, IsExisting={isExistingFile}, LocalPath={file.LocalPath}", cancellationToken);

                if (!isExistingFile)
                {
                    var pendingFile = file with
                    {
                        SyncStatus = FileSyncStatus.PendingUpload
                    };
                    await _fileMetadataRepository.AddAsync(pendingFile, cancellationToken);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Added pending upload record to database: {file.Name}", cancellationToken);
                }

                if (_currentSessionId is not null)
                {
                    var reason = isExistingFile ? "File changed locally" : "New file";
                    var operationLog = new FileOperationLog(
                        Id: Guid.CreateVersion7().ToString(),
                        SyncSessionId: _currentSessionId,
                        AccountId: accountId,
                        Timestamp: DateTime.UtcNow,
                        Operation: FileOperation.Upload,
                        FilePath: file.Path,
                        LocalPath: file.LocalPath,
                        OneDriveId: existingFile?.Id,
                        FileSize: file.Size,
                        LocalHash: file.LocalHash,
                        RemoteHash: existingFile?.LocalHash,
                        LastModifiedUtc: file.LastModifiedUtc,
                        Reason: reason);
                    await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                }

                var baseCompletedBytes = Interlocked.Read(ref completedBytes);
                var currentActiveUploads = Interlocked.CompareExchange(ref activeUploads, 0, 0);
                var uploadProgress = new Progress<long>(bytesUploaded =>
                {
                    var currentCompletedBytes = baseCompletedBytes + bytesUploaded;
                    var currentCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                    ReportProgress(accountId, SyncStatus.Running, totalFiles, currentCompleted, totalBytes, currentCompletedBytes, filesUploading: currentActiveUploads, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes);
                });

                var uploadedItem = await _graphApiClient.UploadFileAsync(
                    accountId,
                    file.LocalPath,
                    file.Path,
                    uploadProgress,
                    _syncCancellation!.Token);

                if (uploadedItem.LastModifiedDateTime.HasValue && File.Exists(file.LocalPath))
                {
                    File.SetLastWriteTimeUtc(file.LocalPath, uploadedItem.LastModifiedDateTime.Value.UtcDateTime);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Synchronized local timestamp to OneDrive: {file.Name}, OldTime={file.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, NewTime={uploadedItem.LastModifiedDateTime.Value.UtcDateTime:yyyy-MM-dd HH:mm:ss}", cancellationToken);
                }

                var oneDriveTimestamp = uploadedItem.LastModifiedDateTime?.UtcDateTime ?? file.LastModifiedUtc;

                FileMetadata uploadedFile;
                if (isExistingFile)
                {
                    uploadedFile = existingFile! with
                    {
                        Id = uploadedItem.Id ?? existingFile.Id, // Preserve ID if upload doesn't return one
                        CTag = uploadedItem.CTag,
                        ETag = uploadedItem.ETag,
                        LocalPath = file.LocalPath,
                        LocalHash = file.LocalHash,
                        Size = file.Size,
                        LastModifiedUtc = oneDriveTimestamp, // Use OneDrive's timestamp
                        SyncStatus = FileSyncStatus.Synced,
                        LastSyncDirection = SyncDirection.Upload
                    };
                }
                else
                {
                    uploadedFile = file with
                    {
                        Id = uploadedItem.Id ?? throw new InvalidOperationException($"Upload succeeded but no ID returned for {file.Name}"),
                        CTag = uploadedItem.CTag,
                        ETag = uploadedItem.ETag,
                        LastModifiedUtc = oneDriveTimestamp, // Use OneDrive's timestamp
                        SyncStatus = FileSyncStatus.Synced,
                        LastSyncDirection = SyncDirection.Upload
                    };
                }

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Upload successful: {file.Name}, OneDrive ID={uploadedFile.Id}, CTag={uploadedFile.CTag}", cancellationToken);

                await _fileMetadataRepository.UpdateAsync(uploadedFile, cancellationToken);

                Interlocked.Increment(ref completedFiles);
                Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                var finalActiveUploads = Interlocked.CompareExchange(ref activeUploads, 0, 0);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, filesUploading: finalActiveUploads, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes);
            }
            catch (Exception ex)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Upload failed for {file.Name}: {ex.Message}", cancellationToken);

                var failedFile = file with
                {
                    SyncStatus = FileSyncStatus.Failed
                };

                var existingDbFile = !string.IsNullOrEmpty(failedFile.Id)
                    ? await _fileMetadataRepository.GetByIdAsync(failedFile.Id, cancellationToken)
                    : await _fileMetadataRepository.GetByPathAsync(accountId, failedFile.Path, cancellationToken);

                if (existingDbFile is not null)
                {
                    await _fileMetadataRepository.UpdateAsync(failedFile, cancellationToken);
                }
                else
                {
                    await _fileMetadataRepository.AddAsync(failedFile, cancellationToken);
                }

                // Continue with next file (don't fail entire sync)
                Interlocked.Increment(ref completedFiles);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes);
            }
            finally
            {
                Interlocked.Decrement(ref activeUploads);
                uploadSemaphore.Release();
            }
        }).ToList();
        return uploadTasks;
    }

    private static async Task<(List<FileMetadata> filesToDownload, int totalFiles, long totalBytes, long downloadBytes)> RemoveDuplicatesFromDownloadList(List<FileMetadata> filesToUpload, List<FileMetadata> filesToDownload, int totalFiles, long totalBytes, long downloadBytes, CancellationToken cancellationToken)
    {
        var duplicateDownloads = filesToDownload.GroupBy(f => f.Path).Where(g => g.Count() > 1).ToList();
        if (duplicateDownloads.Count > 0)
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"WARNING: Found {duplicateDownloads.Count} duplicate paths in download list!", cancellationToken);
            foreach (var dup in duplicateDownloads)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"  Duplicate: {dup.Key} appears {dup.Count()} times", cancellationToken);
            }

            filesToDownload = [.. filesToDownload.GroupBy(f => f.Path).Select(g => g.First())];
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"After deduplication: {filesToDownload.Count} files to download", cancellationToken);

            totalFiles = filesToUpload.Count + filesToDownload.Count;
            totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            downloadBytes = filesToDownload.Sum(f => f.Size);
        }

        return (filesToDownload, totalFiles, totalBytes, downloadBytes);
    }

    private static List<FileMetadata> GetFilesToDelete(IReadOnlyList<FileMetadata> existingFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet, HashSet<string> alreadyProcessedDeletions)
    {
        return [.. existingFiles
            .Where(f => !remotePathsSet.Contains(f.Path) &&
                       !localPathsSet.Contains(f.Path) &&
                       !string.IsNullOrWhiteSpace(f.Id) &&
                       !alreadyProcessedDeletions.Contains(f.Id))
            .Where(f => f.Id is not null)];
    }

    private static List<FileMetadata> GetFilesDeletedLocally(List<FileMetadata> allLocalFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet)
    {

        // Detect files deleted locally - these should be deleted from OneDrive
        // A file is considered "deleted locally" if:
        // 1. It exists in DB (existingFiles)
        // 2. It's NOT in the local scan results (deleted from disk)
        // 3. It has been successfully synced (meaning it exists or existed on OneDrive)
        // 4. It has a valid OneDrive ID (can't delete without ID)
        // Note: We check SyncStatus=Synced OR remote contains it, because newly uploaded files
        // might not appear in remote scan immediately due to OneDrive propagation delays
        return [.. allLocalFiles
            .Where(f => !localPathsSet.Contains(f.Path) &&
                       (remotePathsSet.Contains(f.Path) || f.SyncStatus == FileSyncStatus.Synced) &&
                       !string.IsNullOrEmpty(f.Id))];
    }

    private static List<FileMetadata> SelectFilesDeletedFromOneDriveButSyncedLocally(IReadOnlyList<FileMetadata> existingFiles, HashSet<string> remotePathsSet, HashSet<string> localPathsSet)
    {
        return [.. existingFiles
            .Where(f => !remotePathsSet.Contains(f.Path) &&
                       localPathsSet.Contains(f.Path) &&
                       f.SyncStatus == FileSyncStatus.Synced)];
    }

    private async Task<List<FileMetadata>> GetAllLocalFiles(string accountId, IReadOnlyList<string> selectedFolders, AccountInfo account)
    {
        var allLocalFiles = new List<FileMetadata>();
        foreach (var folder in selectedFolders)
        {
            if (string.IsNullOrEmpty(folder))
                continue;
            var localFolderPath = Path.Combine(account.LocalSyncPath, folder.TrimStart('/'));
            var localFiles = await _localFileScanner.ScanFolderAsync(
                accountId,
                localFolderPath,
                folder,
                _syncCancellation?.Token ?? CancellationToken.None);
            if (localFiles?.Count > 0)
            {
                allLocalFiles.AddRange(localFiles);
            }
        }

        return allLocalFiles;
    }

    private void ResetBeforeRunning()
    {
        _lastProgressUpdate = DateTime.UtcNow;
        _lastCompletedBytes = 0;
        _transferHistory.Clear();
    }

    private bool SyncIsAlreadyRunning() => Interlocked.CompareExchange(ref _syncInProgress, 1, 0) != 0;

    private async Task FinalizeSyncSessionAsync(string? sessionId, int uploadCount, int downloadCount, int deleteCount, int conflictCount, long completedBytes, AccountInfo account, CancellationToken cancellationToken)
    {
        if (sessionId is null)
            return;

        try
        {
            var session = await _syncSessionLogRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session is not null)
            {
                var updatedSession = session with
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncEngine] Failed to finalize sync session log: {ex.Message}");
        }
        finally
        {
            _currentSessionId = null;
        }
    }

    private async Task HandleSyncCancelledAsync(string? sessionId, CancellationToken cancellationToken)
    {
        if (sessionId is null)
            return;

        var session = await _syncSessionLogRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is not null)
        {
            var updatedSession = session with { CompletedUtc = DateTime.UtcNow, Status = SyncStatus.Paused };
            await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
        }
    }

    private async Task HandleSyncFailedAsync(string? sessionId, CancellationToken cancellationToken)
    {
        if (sessionId is null)
            return;

        var session = await _syncSessionLogRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is not null)
        {
            var updatedSession = session with { CompletedUtc = DateTime.UtcNow, Status = SyncStatus.Failed };
            await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
        }
    }

    private async Task UpdateLastAccountSyncAsync(AccountInfo account, CancellationToken cancellationToken)
    {
        var lastSyncUpdate = account with
        {
            LastSyncUtc = DateTime.UtcNow
        };

        await _accountRepository.UpdateAsync(lastSyncUpdate, cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopSyncAsync()
    {
        _syncCancellation?.Cancel();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        return await _syncConflictRepository.GetUnresolvedByAccountIdAsync(accountId, cancellationToken);
    }

    private void ReportProgress(
        string accountId,
        SyncStatus status,
        int totalFiles,
        int completedFiles,
        long totalBytes,
        long completedBytes,
        int filesDownloading = 0,
        int filesUploading = 0,
        int filesDeleted = 0,
        int conflictsDetected = 0,
        string? currentScanningFolder = null,
        long? phaseTotalBytes = null)
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastProgressUpdate).TotalSeconds;

        // Calculate transfer speed (MB/s)
        double megabytesPerSecond = 0;
        if (elapsedSeconds > 0.1) // Only calculate if meaningful time has passed
        {
            var bytesDelta = completedBytes - _lastCompletedBytes;
            if (bytesDelta > 0)
            {
                var megabytesDelta = bytesDelta / (1024.0 * 1024.0);
                megabytesPerSecond = megabytesDelta / elapsedSeconds;

                // Add to transfer history for smoothing (keep last 10 samples)
                _transferHistory.Add((now, completedBytes));
                if (_transferHistory.Count > 10)
                {
                    _transferHistory.RemoveAt(0);
                }

                // Calculate average speed from history for smoother display
                if (_transferHistory.Count >= 2)
                {
                    var totalElapsed = (now - _transferHistory[0].Timestamp).TotalSeconds;
                    var totalTransferred = completedBytes - _transferHistory[0].Bytes;
                    if (totalElapsed > 0)
                    {
                        megabytesPerSecond = totalTransferred / (1024.0 * 1024.0) / totalElapsed;
                    }
                }

                _lastProgressUpdate = now;
                _lastCompletedBytes = completedBytes;
            }
        }

        // Calculate ETA (Estimated Time of Arrival)
        // Use phase-specific bytes if provided (for accurate per-phase ETA)
        int? estimatedSecondsRemaining = null;
        var bytesForEta = phaseTotalBytes ?? totalBytes;
        if (megabytesPerSecond > 0.01 && completedBytes < bytesForEta)
        {
            var remainingBytes = bytesForEta - completedBytes;
            var remainingMegabytes = remainingBytes / (1024.0 * 1024.0);
            estimatedSecondsRemaining = (int)Math.Ceiling(remainingMegabytes / megabytesPerSecond);
        }

        var progress = new SyncState(
            AccountId: accountId,
            Status: status,
            TotalFiles: totalFiles,
            CompletedFiles: completedFiles,
            TotalBytes: totalBytes,
            CompletedBytes: completedBytes,
            FilesDownloading: filesDownloading,
            FilesUploading: filesUploading,
            FilesDeleted: filesDeleted,
            ConflictsDetected: conflictsDetected,
            MegabytesPerSecond: megabytesPerSecond,
            EstimatedSecondsRemaining: estimatedSecondsRemaining,
            CurrentScanningFolder: currentScanningFolder,
            LastUpdateUtc: now);

        _progressSubject.OnNext(progress);
    }

    /// <summary>
    /// Formats a folder path for display by removing Graph API prefixes.
    /// </summary>
    public static string? FormatScanningFolderForDisplay(string? folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return folderPath;
        }

        // Remove /drives/{id}/root: prefix
        var cleaned = MyRegex().Replace(folderPath, string.Empty);

        // Remove /drive/root: prefix
        if (cleaned.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["/drive/root:".Length..];
        }

        // Ensure starts with /
        if (!string.IsNullOrEmpty(cleaned) && !cleaned.StartsWith('/'))
        {
            cleaned = "/" + cleaned;
        }

        return $"OneDrive: {cleaned}";
    }

    public void Dispose()
    {
        _syncCancellation?.Dispose();
        _progressSubject.Dispose();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^/drives/[^/]+/root:")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
