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
    private int _syncInProgress; // 0 = not syncing, 1 = syncing

    // Progress tracking for speed/ETA calculations
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
        ArgumentNullException.ThrowIfNull(localFileScanner);
        ArgumentNullException.ThrowIfNull(remoteChangeDetector);
        ArgumentNullException.ThrowIfNull(fileMetadataRepository);
        ArgumentNullException.ThrowIfNull(syncConfigurationRepository);
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(graphApiClient);
        ArgumentNullException.ThrowIfNull(syncConflictRepository);
        ArgumentNullException.ThrowIfNull(syncSessionLogRepository);
        ArgumentNullException.ThrowIfNull(fileOperationLogRepository);

        _localFileScanner = localFileScanner;
        _remoteChangeDetector = remoteChangeDetector;
        _fileMetadataRepository = fileMetadataRepository;
        _syncConfigurationRepository = syncConfigurationRepository;
        _accountRepository = accountRepository;
        _graphApiClient = graphApiClient;
        _syncConflictRepository = syncConflictRepository;
        _syncSessionLogRepository = syncSessionLogRepository;
        _fileOperationLogRepository = fileOperationLogRepository;

        var initialState = new SyncState(
            AccountId: string.Empty,
            Status: SyncStatus.Idle,
            TotalFiles: 0,
            CompletedFiles: 0,
            TotalBytes: 0,
            CompletedBytes: 0,
            FilesDownloading: 0,
            FilesUploading: 0,
            FilesDeleted: 0,
            ConflictsDetected: 0,
            MegabytesPerSecond: 0,
            EstimatedSecondsRemaining: null,
            CurrentScanningFolder: null,
            LastUpdateUtc: null);

        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }

    /// <inheritdoc/>
    public IObservable<SyncState> Progress => _progressSubject;

    /// <inheritdoc/>
    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        // Prevent concurrent syncs using Interlocked for thread-safety
        if (Interlocked.CompareExchange(ref _syncInProgress, 1, 0) != 0)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncEngine] Sync already in progress for account {accountId}, ignoring duplicate request");
            return;
        }

        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Set debug log context for this account - flows through all async operations
            DebugLogContext.SetAccountId(accountId);

            // Reset progress tracking for new sync
            _lastProgressUpdate = DateTime.UtcNow;
            _lastCompletedBytes = 0;
            _transferHistory.Clear();

            await DebugLog.EntryAsync("SyncEngine.StartSyncAsync", cancellationToken);

            ReportProgress(accountId, SyncStatus.Running, 0, 0, 0, 0);

            // Get selected folders for this account
            var selectedFolders = await _syncConfigurationRepository.GetSelectedFoldersAsync(accountId, cancellationToken);

            // Deduplicate selected folders to prevent processing same folder multiple times
            selectedFolders = [.. selectedFolders.Distinct()];

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Starting sync with {selectedFolders.Count} selected folders: {string.Join(", ", selectedFolders)}", cancellationToken);

            if (selectedFolders.Count == 0)
            {
                ReportProgress(accountId, SyncStatus.Idle, 0, 0, 0, 0);
                return;
            }

            // Get account info for local sync path
            var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
            {
                ReportProgress(accountId, SyncStatus.Failed, 0, 0, 0, 0);
                return;
            }

            // Initialize detailed sync logging if enabled
            if (account.EnableDetailedSyncLogging)
            {
                var sessionLog = new SyncSessionLog(
                    Id: Guid.NewGuid().ToString(),
                    AccountId: accountId,
                    StartedUtc: DateTime.UtcNow,
                    CompletedUtc: null,
                    Status: SyncStatus.Running,
                    FilesUploaded: 0,
                    FilesDownloaded: 0,
                    FilesDeleted: 0,
                    ConflictsDetected: 0,
                    TotalBytes: 0);
                await _syncSessionLogRepository.AddAsync(sessionLog, cancellationToken);
                _currentSessionId = sessionLog.Id;
            }
            else
            {
                _currentSessionId = null;
            }

            // Scan local files in selected folders
            var allLocalFiles = new List<FileMetadata>();
            foreach (var folder in selectedFolders)
            {
                var localFolderPath = Path.Combine(account.LocalSyncPath, folder.TrimStart('/'));
                var localFiles = await _localFileScanner.ScanFolderAsync(
                    accountId,
                    localFolderPath,
                    folder,
                    _syncCancellation.Token);
                allLocalFiles.AddRange(localFiles);
            }

            // Get existing file metadata from database
            var existingFiles = await _fileMetadataRepository.GetByAccountIdAsync(accountId, cancellationToken);

            // Handle duplicate records (data corruption from previous bugs)
            // Group by Path and keep only the most relevant record
            var existingFilesDict = existingFiles
                .GroupBy(f => f.Path)
                .Select(g =>
                {
                    if (g.Count() > 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] WARNING: Found {g.Count()} duplicate records for path {g.Key}");

                        // Keep the record with the best data:
                        // 1. Prefer Synced status over others
                        // 2. Prefer records with OneDrive ID
                        // 3. Prefer records with CTag/ETag
                        // 4. Most recent LastModifiedUtc
                        var best = g
                            .OrderByDescending(f => f.SyncStatus == FileSyncStatus.Synced)
                            .ThenByDescending(f => !string.IsNullOrEmpty(f.Id))
                            .ThenByDescending(f => !string.IsNullOrEmpty(f.CTag))
                            .ThenByDescending(f => f.LastModifiedUtc)
                            .First();

                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] Keeping record with ID={best.Id}, Status={best.SyncStatus}, CTag={best.CTag}");

                        // Delete the duplicate records from database
                        foreach (var duplicate in g.Where(f => f.Id != best.Id))
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncEngine] Deleting duplicate record with ID={duplicate.Id}");
                                _fileMetadataRepository.DeleteAsync(duplicate.Id, cancellationToken).GetAwaiter().GetResult();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncEngine] Failed to delete duplicate: {ex.Message}");
                            }
                        }

                        return best;
                    }
                    return g.First();
                })
                .ToDictionary(f => f.Path, f => f);

            // Detect remote changes in selected folders FIRST (before deciding what to upload)
            var allRemoteFiles = new List<FileMetadata>();
            foreach (var folder in selectedFolders)
            {
                // Report scanning progress with cleaned folder path
                var displayFolder = FormatScanningFolderForDisplay(folder);
                ReportProgress(accountId, SyncStatus.Running, 0, 0, 0, 0, currentScanningFolder: displayFolder);

                var (remoteFiles, _) = await _remoteChangeDetector.DetectChangesAsync(
                    accountId,
                    folder,
                    previousDeltaLink: null,
                    _syncCancellation.Token);
                System.Diagnostics.Debug.WriteLine($"[SyncEngine] Folder '{folder}' returned {remoteFiles.Count} remote files");
                allRemoteFiles.AddRange(remoteFiles);
            }

            System.Diagnostics.Debug.WriteLine($"[SyncEngine] Total remote files before deduplication: {allRemoteFiles.Count}");

            // Deduplicate remote files by Path (in case overlapping folder selections return same files)
            allRemoteFiles = [.. allRemoteFiles
                .GroupBy(f => f.Path)
                .Select(g => g.First())];

            System.Diagnostics.Debug.WriteLine($"[SyncEngine] Total remote files after deduplication: {allRemoteFiles.Count}");
            System.Diagnostics.Debug.WriteLine($"[SyncEngine] Remote file paths: {string.Join(", ", allRemoteFiles.Select(f => f.Path))}");

            // Create dictionaries for fast lookup
            var remoteFilesDict = allRemoteFiles.ToDictionary(f => f.Path, f => f);
            var localFilesDict = allLocalFiles.ToDictionary(f => f.Path, f => f);

            // Determine which files need uploading
            var filesToUpload = new List<FileMetadata>();
            foreach (var localFile in allLocalFiles)
            {
                if (existingFilesDict.TryGetValue(localFile.Path, out var existingFile))
                {
                    // File exists in DB - check if it needs uploading

                    // Case 1: File has pending upload or failed status - needs (re)upload
                    if (existingFile.SyncStatus is FileSyncStatus.PendingUpload or
                        FileSyncStatus.Failed)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] File needs upload (status={existingFile.SyncStatus}): {localFile.Name}");
                        // Use existing DB record (preserves ID and status) but update with current local file info
                        var fileToUpload = existingFile with
                        {
                            LocalPath = localFile.LocalPath,
                            LocalHash = localFile.LocalHash,
                            Size = localFile.Size,
                            LastModifiedUtc = localFile.LastModifiedUtc
                        };
                        filesToUpload.Add(fileToUpload);
                    }
                    // Case 2: File was synced - check if it changed locally
                    else
                    {
                        var bothHaveHashes = !string.IsNullOrEmpty(existingFile.LocalHash) &&
                                            !string.IsNullOrEmpty(localFile.LocalHash);

                        bool hasChanged;
                        if (bothHaveHashes)
                        {
                            hasChanged = existingFile.LocalHash != localFile.LocalHash;
                            if (hasChanged)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncEngine] File marked as changed: {localFile.Name}");
                                System.Diagnostics.Debug.WriteLine($"  Hash changed (DB: {existingFile.LocalHash}, Local: {localFile.LocalHash})");
                            }
                        }
                        else
                        {
                            hasChanged = existingFile.Size != localFile.Size;
                            if (hasChanged)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncEngine] File marked as changed: {localFile.Name}");
                                System.Diagnostics.Debug.WriteLine($"  Size changed (DB: {existingFile.Size}, Local: {localFile.Size})");
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
                    // File NOT in DB AND NOT on remote - new local file, needs upload
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] New local file to upload: {localFile.Name}");
                    filesToUpload.Add(localFile);
                }
                // If file NOT in DB BUT exists on remote - will be handled in conflict detection below
            }

            // Determine which files need downloading and detect conflicts
            var filesToDownload = new List<FileMetadata>();
            var remotePathsSet = allRemoteFiles.Select(f => f.Path).ToHashSet();
            var conflictCount = 0;
            var conflictPaths = new HashSet<string>();
            var filesToRecordWithoutTransfer = new List<FileMetadata>(); // Files that match, just need DB record

            foreach (var remoteFile in allRemoteFiles)
            {
                if (existingFilesDict.TryGetValue(remoteFile.Path, out var existingFile))
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Found file in DB: {remoteFile.Path}, DB Status={existingFile.SyncStatus}");
                    // File exists in DB - check if remote changed
                    var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                    var remoteHasChanged = existingFile.CTag != remoteFile.CTag ||
                                         timeDiff > 1.0 ||
                                         existingFile.Size != remoteFile.Size;

                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Remote file check: {remoteFile.Path}");
                    System.Diagnostics.Debug.WriteLine($"  DB CTag={existingFile.CTag}, Remote CTag={remoteFile.CTag}");
                    System.Diagnostics.Debug.WriteLine($"  DB Time={existingFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Diff={timeDiff:F1}s");
                    System.Diagnostics.Debug.WriteLine($"  DB Size={existingFile.Size}, Remote Size={remoteFile.Size}");
                    System.Diagnostics.Debug.WriteLine($"  RemoteHasChanged={remoteHasChanged}");

                    if (remoteHasChanged)
                    {
                        // Check if local file also changed - conflict detection
                        var localFileHasChanged = false;

                        if (localFilesDict.TryGetValue(remoteFile.Path, out var localFile))
                        {
                            var localTimeDiff = Math.Abs((existingFile.LastModifiedUtc - localFile.LastModifiedUtc).TotalSeconds);
                            localFileHasChanged = localTimeDiff > 1.0 || existingFile.Size != localFile.Size;
                        }

                        if (localFileHasChanged)
                        {
                            // CONFLICT: Both local and remote changed!
                            var localFileFromDict = localFilesDict[remoteFile.Path];
                            var conflict = new SyncConflict(
                                Id: Guid.NewGuid().ToString(),
                                AccountId: accountId,
                                FilePath: remoteFile.Path,
                                LocalModifiedUtc: localFileFromDict.LastModifiedUtc,
                                RemoteModifiedUtc: remoteFile.LastModifiedUtc,
                                LocalSize: localFileFromDict.Size,
                                RemoteSize: remoteFile.Size,
                                DetectedUtc: DateTime.UtcNow,
                                ResolutionStrategy: ConflictResolutionStrategy.None,
                                IsResolved: false);

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
                            System.Diagnostics.Debug.WriteLine($"[SyncEngine] CONFLICT detected for {remoteFile.Path}: local and remote both changed");

                            // Log conflict detection if detailed logging is enabled
                            if (_currentSessionId is not null)
                            {
                                var operationLog = new FileOperationLog(
                                    Id: Guid.NewGuid().ToString(),
                                    SyncSessionId: _currentSessionId,
                                    AccountId: accountId,
                                    Timestamp: DateTime.UtcNow,
                                    Operation: FileOperation.ConflictDetected,
                                    FilePath: remoteFile.Path,
                                    LocalPath: localFileFromDict.LocalPath,
                                    OneDriveId: remoteFile.Id,
                                    FileSize: localFileFromDict.Size,
                                    LocalHash: localFileFromDict.LocalHash,
                                    RemoteHash: null,
                                    LastModifiedUtc: localFileFromDict.LastModifiedUtc,
                                    Reason: $"Conflict: Both local and remote changed. Local modified: {localFileFromDict.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}");
                                await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                            }
                            continue;
                        }

                        // Remote changed but local didn't - download
                        var localFilePath = Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                        var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                        filesToDownload.Add(fileWithLocalPath);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] File NOT in DB: {remoteFile.Path} - first sync or new file");
                    // No DB record - first sync or new file
                    if (localFilesDict.TryGetValue(remoteFile.Path, out var localFile))
                    {
                        // File exists BOTH locally and remotely - compare them
                        var timeDiff = Math.Abs((localFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                        // Use 60 second tolerance - OneDrive may round timestamps to nearest minute
                        var filesMatch = localFile.Size == remoteFile.Size && timeDiff <= 60.0;

                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] First sync compare: {remoteFile.Path}");
                        System.Diagnostics.Debug.WriteLine($"  Local:  Size={localFile.Size}, Time={localFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}");
                        System.Diagnostics.Debug.WriteLine($"  Remote: Size={remoteFile.Size}, Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}");
                        System.Diagnostics.Debug.WriteLine($"  TimeDiff={timeDiff:F1}s, Match={filesMatch}");

                        if (filesMatch)
                        {
                            // Files match - just record in DB, no upload/download needed
                            System.Diagnostics.Debug.WriteLine($"[SyncEngine] File exists both places and matches: {remoteFile.Path} - recording in DB");
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
                            // Files differ - CONFLICT on first sync!
                            System.Diagnostics.Debug.WriteLine($"[SyncEngine] First sync CONFLICT: {remoteFile.Path} - files differ (TimeDiff={timeDiff:F1}s, SizeMatch={localFile.Size == remoteFile.Size})");
                            var conflict = new SyncConflict(
                                Id: Guid.NewGuid().ToString(),
                                AccountId: accountId,
                                FilePath: remoteFile.Path,
                                LocalModifiedUtc: localFile.LastModifiedUtc,
                                RemoteModifiedUtc: remoteFile.LastModifiedUtc,
                                LocalSize: localFile.Size,
                                RemoteSize: remoteFile.Size,
                                DetectedUtc: DateTime.UtcNow,
                                ResolutionStrategy: ConflictResolutionStrategy.None,
                                IsResolved: false);

                            await _syncConflictRepository.AddAsync(conflict, cancellationToken);
                            conflictCount++;
                            conflictPaths.Add(remoteFile.Path);

                            // Log conflict detection if detailed logging is enabled
                            if (_currentSessionId is not null)
                            {
                                var operationLog = new FileOperationLog(
                                    Id: Guid.NewGuid().ToString(),
                                    SyncSessionId: _currentSessionId,
                                    AccountId: accountId,
                                    Timestamp: DateTime.UtcNow,
                                    Operation: FileOperation.ConflictDetected,
                                    FilePath: remoteFile.Path,
                                    LocalPath: localFile.LocalPath,
                                    OneDriveId: remoteFile.Id,
                                    FileSize: localFile.Size,
                                    LocalHash: localFile.LocalHash,
                                    RemoteHash: null,
                                    LastModifiedUtc: localFile.LastModifiedUtc,
                                    Reason: $"First sync conflict: Files differ. Local: Size={localFile.Size}, Time={localFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}. Remote: Size={remoteFile.Size}, Time={remoteFile.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}. TimeDiff={timeDiff:F1}s");
                                await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        // New remote file - download
                        var localFilePath = Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                        var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                        filesToDownload.Add(fileWithLocalPath);
                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] New remote file to download: {remoteFile.Path}");
                    }
                }
            }

            // Save matching files to DB without transferring them
            foreach (var file in filesToRecordWithoutTransfer)
            {
                await _fileMetadataRepository.AddAsync(file, cancellationToken);
            }

            // Detect files deleted from OneDrive - delete local copies to maintain sync
            // BUT exclude files that haven't been successfully synced yet (PendingUpload, PendingDownload, Failed)
            // Only delete files that were previously Synced (meaning they existed on OneDrive at some point)
            var localPathsSet = allLocalFiles.Select(f => f.Path).ToHashSet();
            var deletedFromOneDrive = existingFiles
                .Where(f => !remotePathsSet.Contains(f.Path) &&
                           localPathsSet.Contains(f.Path) &&
                           f.SyncStatus == FileSyncStatus.Synced)
                .ToList();

            foreach (var file in deletedFromOneDrive)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] File deleted from OneDrive: {file.Path} - deleting local copy at {file.LocalPath}");
                    if (File.Exists(file.LocalPath))
                    {
                        File.Delete(file.LocalPath);
                    }
                    await _fileMetadataRepository.DeleteAsync(file.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Failed to delete local file {file.Path}: {ex.Message}");
                    // Continue with other deletions even if one fails
                }
            }

            // Detect files deleted locally - these should be deleted from OneDrive
            // A file is considered "deleted locally" if:
            // 1. It exists in DB (existingFiles)
            // 2. It's NOT in the local scan results (deleted from disk)
            // 3. It has been successfully synced (meaning it exists or existed on OneDrive)
            // 4. It has a valid OneDrive ID (can't delete without ID)
            // Note: We check SyncStatus=Synced OR remote contains it, because newly uploaded files
            // might not appear in remote scan immediately due to OneDrive propagation delays
            var deletedLocally = existingFiles
                .Where(f => !localPathsSet.Contains(f.Path) &&
                           (remotePathsSet.Contains(f.Path) || f.SyncStatus == FileSyncStatus.Synced) &&
                           !string.IsNullOrEmpty(f.Id))
                .ToList();

            foreach (var file in deletedLocally)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] File deleted locally: {file.Path} - deleting from OneDrive (ID: {file.Id})");
                    await _graphApiClient.DeleteFileAsync(accountId, file.Id, cancellationToken);
                    await _fileMetadataRepository.DeleteAsync(file.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Failed to delete from OneDrive {file.Path}: {ex.Message}");
                    // Continue with other deletions even if one fails
                }
            }

            // Detect deletions: files in database but deleted from BOTH remote AND local
            // Exclude files already handled by deletedFromOneDrive or deletedLocally
            var alreadyProcessedDeletions = deletedFromOneDrive.Select(f => f.Id)
                .Concat(deletedLocally.Select(f => f.Id))
                .ToHashSet();

            var filesToDelete = existingFiles
                .Where(f => !remotePathsSet.Contains(f.Path) &&
                           !localPathsSet.Contains(f.Path) &&
                           !alreadyProcessedDeletions.Contains(f.Id))
                .ToList();

            // Detect conflicts: files that appear in both upload and download lists
            var uploadPathsSet = filesToUpload.Select(f => f.Path).ToHashSet();
            // Remove files from upload list that were deleted from OneDrive (since they were deleted locally)
            var deletedPaths = deletedFromOneDrive.Select(f => f.Path).ToHashSet();
            filesToUpload = [.. filesToUpload.Where(f => !deletedPaths.Contains(f.Path) && !conflictPaths.Contains(f.Path))];

            var totalFiles = filesToUpload.Count + filesToDownload.Count;
            var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            var uploadBytes = filesToUpload.Sum(f => f.Size);
            var downloadBytes = filesToDownload.Sum(f => f.Size);

            // Log file counts for debugging
            System.Diagnostics.Debug.WriteLine($"Sync summary: {filesToDownload.Count} to download, {filesToUpload.Count} to upload, {filesToDelete.Count} to delete");
            System.Diagnostics.Debug.WriteLine($"[SyncEngine] Files to download: {string.Join(", ", filesToDownload.Select(f => f.Path))}");

            // Check for duplicates in download list
            var duplicateDownloads = filesToDownload.GroupBy(f => f.Path).Where(g => g.Count() > 1).ToList();
            if (duplicateDownloads.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncEngine] WARNING: Found {duplicateDownloads.Count} duplicate paths in download list!");
                foreach (var dup in duplicateDownloads)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine]   Duplicate: {dup.Key} appears {dup.Count()} times");
                }

                // Deduplicate: Keep first occurrence of each path
                filesToDownload = [.. filesToDownload.GroupBy(f => f.Path).Select(g => g.First())];
                System.Diagnostics.Debug.WriteLine($"[SyncEngine] After deduplication: {filesToDownload.Count} files to download");

                // Recalculate totals after deduplication
                totalFiles = filesToUpload.Count + filesToDownload.Count;
                totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
                downloadBytes = filesToDownload.Sum(f => f.Size);
            }

            ReportProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes, 0, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            var completedFiles = 0;
            long completedBytes = 0;

            // Upload files to OneDrive with parallel execution
            // Ensure we have a valid value (defensive check in case of data migration issues)
            var maxParallelUploads = Math.Max(1, account.MaxParallelUpDownloads);
            using var uploadSemaphore = new SemaphoreSlim(maxParallelUploads, maxParallelUploads);
            var activeUploads = 0;
            var uploadTasks = filesToUpload.Select(async file =>
            {
                await uploadSemaphore.WaitAsync(_syncCancellation.Token);
                Interlocked.Increment(ref activeUploads);

                try
                {
                    _syncCancellation.Token.ThrowIfCancellationRequested();

                    // Determine if this is an update or new file
                    // Check if file exists in DB with PendingUpload/Failed status (resuming upload)
                    // OR exists in DB with a valid OneDrive ID (updating existing file)
                    var isExistingFile = existingFilesDict.TryGetValue(file.Path, out var existingFile) &&
                        (!string.IsNullOrEmpty(existingFile.Id) ||
                         existingFile.SyncStatus == FileSyncStatus.PendingUpload ||
                         existingFile.SyncStatus == FileSyncStatus.Failed);

                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Uploading {file.Name}: Path={file.Path}, IsExisting={isExistingFile}, LocalPath={file.LocalPath}");

                    // Save file to database BEFORE upload starts (with PendingUpload status)
                    // This prevents the file from being deleted if sync is cancelled mid-upload
                    // But ONLY if the file doesn't already exist in DB (avoid duplicate insert)
                    if (!isExistingFile)
                    {
                        var pendingFile = file with
                        {
                            SyncStatus = FileSyncStatus.PendingUpload
                        };
                        await _fileMetadataRepository.AddAsync(pendingFile, cancellationToken);
                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] Added pending upload record to database: {file.Name}");
                    }

                    // Log file operation if detailed logging is enabled
                    if (_currentSessionId is not null)
                    {
                        var reason = isExistingFile ? "File changed locally" : "New file";
                        var operationLog = new FileOperationLog(
                            Id: Guid.NewGuid().ToString(),
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

                    // Upload file to OneDrive via Graph API with progress reporting
                    var baseCompletedBytes = Interlocked.Read(ref completedBytes);
                    var currentActiveUploads = Interlocked.CompareExchange(ref activeUploads, 0, 0);
                    var uploadProgress = new Progress<long>(bytesUploaded =>
                    {
                        // Update completedBytes with current upload progress
                        var currentCompletedBytes = baseCompletedBytes + bytesUploaded;
                        var currentCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                        ReportProgress(accountId, SyncStatus.Running, totalFiles, currentCompleted, totalBytes, currentCompletedBytes, filesUploading: currentActiveUploads, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes);
                    });

                    var uploadedItem = await _graphApiClient.UploadFileAsync(
                        accountId,
                        file.LocalPath,
                        file.Path,
                        uploadProgress,
                        _syncCancellation.Token);

                    // Synchronize local file timestamp to match OneDrive's timestamp
                    // This prevents false "file changed" detection on next sync
                    if (uploadedItem.LastModifiedDateTime.HasValue && File.Exists(file.LocalPath))
                    {
                        File.SetLastWriteTimeUtc(file.LocalPath, uploadedItem.LastModifiedDateTime.Value.UtcDateTime);
                        System.Diagnostics.Debug.WriteLine($"[SyncEngine] Synchronized local timestamp to OneDrive: {file.Name}, " +
                            $"OldTime={file.LastModifiedUtc:yyyy-MM-dd HH:mm:ss}, NewTime={uploadedItem.LastModifiedDateTime.Value.UtcDateTime:yyyy-MM-dd HH:mm:ss}");
                    }

                    // Use OneDrive's timestamp in database to match the file system
                    var oneDriveTimestamp = uploadedItem.LastModifiedDateTime?.UtcDateTime ?? file.LastModifiedUtc;

                    // Update file metadata with uploaded status and OneDrive metadata
                    FileMetadata uploadedFile;
                    if (isExistingFile)
                    {
                        // For existing files, update with new OneDrive metadata from upload
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
                        // New file - use OneDrive ID and metadata from upload response
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

                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Upload successful: {file.Name}, OneDrive ID={uploadedFile.Id}, CTag={uploadedFile.CTag}");

                    // Save to database - always UPDATE because we either:
                    // 1. Saved as PendingUpload before upload started, OR
                    // 2. File already existed in DB (isExistingFile = true)
                    // So the record should already exist - never ADD after upload
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
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Upload failed for {file.Name}: {ex.Message}");

                    // Mark file as failed in database
                    var failedFile = file with
                    {
                        SyncStatus = FileSyncStatus.Failed
                    };

                    // Check if file exists in DB (by ID or by path) before adding
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

            await Task.WhenAll(uploadTasks);

            // Reset transfer tracking for download phase
            _transferHistory.Clear();
            _lastProgressUpdate = DateTime.UtcNow;
            _lastCompletedBytes = completedBytes;

            // Download files with parallel execution
            // Ensure we have a valid value (defensive check in case of data migration issues)
            var maxParallelDownloads = Math.Max(1, account.MaxParallelUpDownloads);
            using var downloadSemaphore = new SemaphoreSlim(maxParallelDownloads, maxParallelDownloads);
            var activeDownloads = 0;
            var downloadTasks = filesToDownload.Select(async file =>
            {
                await downloadSemaphore.WaitAsync(_syncCancellation.Token);
                Interlocked.Increment(ref activeDownloads);

                try
                {
                    _syncCancellation.Token.ThrowIfCancellationRequested();

                    System.Diagnostics.Debug.WriteLine($"Starting download: {file.Name} (ID: {file.Id}) to {file.LocalPath}");
                    await DebugLog.InfoAsync("SyncEngine.DownloadFile", $"Starting download: {file.Name} (ID: {file.Id}) to {file.LocalPath}", _syncCancellation.Token);

                    // Create directory if it doesn't exist
                    var directory = Path.GetDirectoryName(file.LocalPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        await DebugLog.InfoAsync("SyncEngine.DownloadFile", $"Creating directory: {directory}", _syncCancellation.Token);
                        Directory.CreateDirectory(directory);
                    }

                    // Log file operation if detailed logging is enabled
                    if (_currentSessionId is not null)
                    {
                        var existingLocal = existingFilesDict.TryGetValue(file.Path, out var existingFile);
                        var reason = existingLocal ? "Remote file changed" : "New remote file";
                        var operationLog = new FileOperationLog(
                            Id: Guid.NewGuid().ToString(),
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

                    // Download file from OneDrive using Graph API
                    await _graphApiClient.DownloadFileAsync(accountId, file.Id, file.LocalPath, _syncCancellation.Token);

                    System.Diagnostics.Debug.WriteLine($"Download complete: {file.Name}, computing hash...");
                    await DebugLog.InfoAsync("SyncEngine.DownloadFile", $"Download complete: {file.Name}, computing hash...", _syncCancellation.Token);

                    // Compute hash of downloaded file
                    var downloadedHash = await _localFileScanner.ComputeFileHashAsync(file.LocalPath, _syncCancellation.Token);

                    System.Diagnostics.Debug.WriteLine($"Hash computed for {file.Name}: {downloadedHash}");

                    // Update file metadata with downloaded status
                    var downloadedFile = file with
                    {
                        SyncStatus = FileSyncStatus.Synced,
                        LastSyncDirection = SyncDirection.Download,
                        LocalHash = downloadedHash
                    };

                    // Save to database - check if record exists
                    FileMetadata? existingRecord = null;
                    if (!string.IsNullOrEmpty(downloadedFile.Id))
                    {
                        existingRecord = await _fileMetadataRepository.GetByIdAsync(downloadedFile.Id, cancellationToken);
                    }
                    // If not found by ID, try by path (for files that might have empty IDs)
                    existingRecord ??= await _fileMetadataRepository.GetByPathAsync(accountId, downloadedFile.Path, cancellationToken);

                    await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Saving {file.Name}: ExistingRecord={(existingRecord is not null ? "Found" : "NotFound")}, ID={downloadedFile.Id}, Path={downloadedFile.Path}", _syncCancellation.Token);

                    try
                    {
                        if (existingRecord is not null)
                        {
                            await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Updating existing record: ExistingID={existingRecord.Id}, NewID={downloadedFile.Id}", _syncCancellation.Token);
                            await _fileMetadataRepository.UpdateAsync(downloadedFile, cancellationToken);
                        }
                        else
                        {
                            await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Adding new record: ID={downloadedFile.Id}", _syncCancellation.Token);
                            await _fileMetadataRepository.AddAsync(downloadedFile, cancellationToken);
                        }
                        await DebugLog.InfoAsync("SyncEngine.SaveFileMetadata", $"Successfully saved {file.Name} to database", _syncCancellation.Token);
                    }
                    catch (Exception dbEx)
                    {
                        await DebugLog.ErrorAsync("SyncEngine.SaveFileMetadata", $"FAILED to save {file.Name} to database: {dbEx.Message}", dbEx, _syncCancellation.Token);
                        throw; // Re-throw to trigger outer catch handler
                    }

                    System.Diagnostics.Debug.WriteLine($"Successfully synced: {file.Name}");

                    Interlocked.Increment(ref completedFiles);
                    Interlocked.Add(ref completedBytes, file.Size);
                    var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                    var finalBytes = Interlocked.Read(ref completedBytes);
                    var finalActiveDownloads = Interlocked.CompareExchange(ref activeDownloads, 0, 0);
                    ReportProgress(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, filesDownloading: finalActiveDownloads, conflictsDetected: conflictCount, phaseTotalBytes: uploadBytes + downloadBytes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR downloading {file.Name}: {ex.GetType().Name} - {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    await DebugLog.ErrorAsync("SyncEngine.DownloadFile", $"ERROR downloading {file.Name}: {ex.Message}", ex, _syncCancellation.Token);

                    // Log download failure and mark file as failed
                    var failedFile = file with { SyncStatus = FileSyncStatus.Failed };

                    // Check if file exists in DB (by ID or by path) before adding
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

            await Task.WhenAll(downloadTasks);

            // Handle deletions
            foreach (var fileToDelete in filesToDelete)
            {
                await _fileMetadataRepository.DeleteAsync(fileToDelete.Id, cancellationToken);
            }

            ReportProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes, completedBytes, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", $"Sync completed: {completedFiles}/{totalFiles} files, {completedBytes} bytes", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", cancellationToken);

            // Finalize sync session log if detailed logging is enabled
            if (_currentSessionId is not null)
            {
                try
                {
                    var session = await _syncSessionLogRepository.GetByIdAsync(_currentSessionId, cancellationToken);
                    if (session is not null)
                    {
                        var updatedSession = session with
                        {
                            CompletedUtc = DateTime.UtcNow,
                            Status = SyncStatus.Completed,
                            FilesUploaded = filesToUpload.Count,
                            FilesDownloaded = filesToDownload.Count,
                            FilesDeleted = filesToDelete.Count,
                            ConflictsDetected = conflictCount,
                            TotalBytes = completedBytes
                        };
                        await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Failed to finalize sync session log: {ex.Message}");
                    // Don't fail the entire sync if session log update fails
                }
                finally
                {
                    _currentSessionId = null;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Finalize sync session log as paused if detailed logging is enabled
            if (_currentSessionId is not null)
            {
                var session = await _syncSessionLogRepository.GetByIdAsync(_currentSessionId, cancellationToken);
                if (session is not null)
                {
                    var updatedSession = session with
                    {
                        CompletedUtc = DateTime.UtcNow,
                        Status = SyncStatus.Paused
                    };
                    await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
                }
                _currentSessionId = null;
            }
            ReportProgress(accountId, SyncStatus.Paused, 0, 0, 0, 0);
            throw;
        }
        catch (Exception ex)
        {
            await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", $"Sync failed: {ex.Message}", ex, cancellationToken);

            // Finalize sync session log as failed if detailed logging is enabled
            if (_currentSessionId is not null)
            {
                var session = await _syncSessionLogRepository.GetByIdAsync(_currentSessionId, cancellationToken);
                if (session is not null)
                {
                    var updatedSession = session with
                    {
                        CompletedUtc = DateTime.UtcNow,
                        Status = SyncStatus.Failed
                    };
                    await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
                }
                _currentSessionId = null;
            }
            ReportProgress(accountId, SyncStatus.Failed, 0, 0, 0, 0);
            throw;
        }
        finally
        {
            // Clear debug log context
            DebugLogContext.Clear();

            // Always reset the sync-in-progress flag
            Interlocked.Exchange(ref _syncInProgress, 0);
        }
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
