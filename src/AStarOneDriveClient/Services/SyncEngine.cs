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
public sealed class SyncEngine : ISyncEngine, IDisposable
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
            LastUpdateUtc: null);

        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }

    /// <inheritdoc/>
    public IObservable<SyncState> Progress => _progressSubject;

    /// <inheritdoc/>
    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            ReportProgress(accountId, SyncStatus.Running, 0, 0, 0, 0);

            // Get selected folders for this account
            var selectedFolders = await _syncConfigurationRepository.GetSelectedFoldersAsync(accountId, cancellationToken);
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
                var localFolderPath = System.IO.Path.Combine(account.LocalSyncPath, folder.TrimStart('/'));
                var localFiles = await _localFileScanner.ScanFolderAsync(
                    accountId,
                    localFolderPath,
                    folder,
                    _syncCancellation.Token);
                allLocalFiles.AddRange(localFiles);
            }

            // Get existing file metadata from database
            var existingFiles = await _fileMetadataRepository.GetByAccountIdAsync(accountId, cancellationToken);
            var existingFilesDict = existingFiles.ToDictionary(f => f.Path, f => f);

            // Detect remote changes in selected folders FIRST (before deciding what to upload)
            var allRemoteFiles = new List<FileMetadata>();
            foreach (var folder in selectedFolders)
            {
                var (remoteFiles, _) = await _remoteChangeDetector.DetectChangesAsync(
                    accountId,
                    folder,
                    previousDeltaLink: null,
                    _syncCancellation.Token);
                allRemoteFiles.AddRange(remoteFiles);
            }

            // Create dictionaries for fast lookup
            var remoteFilesDict = allRemoteFiles.ToDictionary(f => f.Path, f => f);
            var localFilesDict = allLocalFiles.ToDictionary(f => f.Path, f => f);

            // Determine which files need uploading
            var filesToUpload = new List<FileMetadata>();
            foreach (var localFile in allLocalFiles)
            {
                if (existingFilesDict.TryGetValue(localFile.Path, out var existingFile))
                {
                    // File exists in DB - check if it changed locally
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
                    // File exists in DB - check if remote changed
                    var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                    var remoteHasChanged = existingFile.CTag != remoteFile.CTag ||
                                         timeDiff > 1.0 ||
                                         existingFile.Size != remoteFile.Size;

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
                        var localFilePath = System.IO.Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                        var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                        filesToDownload.Add(fileWithLocalPath);
                    }
                }
                else
                {
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
                        var localFilePath = System.IO.Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
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
            var localPathsSet = allLocalFiles.Select(f => f.Path).ToHashSet();
            var deletedFromOneDrive = existingFiles
                .Where(f => !remotePathsSet.Contains(f.Path) && localPathsSet.Contains(f.Path))
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
            var deletedLocally = existingFiles
                .Where(f => remotePathsSet.Contains(f.Path) && !localPathsSet.Contains(f.Path))
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
            var filesToDelete = existingFiles
                .Where(f => !remotePathsSet.Contains(f.Path) && !localPathsSet.Contains(f.Path))
                .ToList();

            // Detect conflicts: files that appear in both upload and download lists
            var uploadPathsSet = filesToUpload.Select(f => f.Path).ToHashSet();
            // Remove files from upload list that were deleted from OneDrive (since they were deleted locally)
            var deletedPaths = deletedFromOneDrive.Select(f => f.Path).ToHashSet();
            filesToUpload = filesToUpload.Where(f => !deletedPaths.Contains(f.Path) && !conflictPaths.Contains(f.Path)).ToList();

            var totalFiles = filesToUpload.Count + filesToDownload.Count;
            var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);

            // Log file counts for debugging
            System.Diagnostics.Debug.WriteLine($"Sync summary: {filesToDownload.Count} to download, {filesToUpload.Count} to upload, {filesToDelete.Count} to delete");

            ReportProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes, 0, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            int completedFiles = 0;
            long completedBytes = 0;

            // Upload files to OneDrive
            for (int i = 0; i < filesToUpload.Count; i++)
            {
                _syncCancellation.Token.ThrowIfCancellationRequested();

                var file = filesToUpload[i];

                try
                {
                    // Determine if this is an update or new file
                    // File must exist in DB AND have an OneDrive ID to be considered "existing"
                    // Files without IDs are "pending first upload" and should use ADD path
                    var isExistingFile = existingFilesDict.TryGetValue(file.Path, out var existingFile)
                        && !string.IsNullOrEmpty(existingFile.Id);

                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Uploading {file.Name}: Path={file.Path}, IsExisting={isExistingFile}, LocalPath={file.LocalPath}");

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

                    // Upload file to OneDrive via Graph API
                    var uploadedItem = await _graphApiClient.UploadFileAsync(
                        accountId,
                        file.LocalPath,
                        file.Path,
                        _syncCancellation.Token);

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
                            LastModifiedUtc = file.LastModifiedUtc, // Keep local timestamp
                            SyncStatus = FileSyncStatus.Synced,
                            LastSyncDirection = SyncDirection.Upload
                        };
                    }
                    else
                    {
                        // New file - use OneDrive ID and metadata from upload response
                        // IMPORTANT: Keep local LastModifiedUtc, not OneDrive's (they may differ slightly)
                        uploadedFile = file with
                        {
                            Id = uploadedItem.Id ?? throw new InvalidOperationException($"Upload succeeded but no ID returned for {file.Name}"),
                            CTag = uploadedItem.CTag,
                            ETag = uploadedItem.ETag,
                            // Keep file.LastModifiedUtc (local timestamp) for accurate comparison in next sync
                            SyncStatus = FileSyncStatus.Synced,
                            LastSyncDirection = SyncDirection.Upload
                        };
                    }

                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] Upload successful: {file.Name}, OneDrive ID={uploadedFile.Id}, CTag={uploadedFile.CTag}");

                    // Save to database
                    if (isExistingFile)
                    {
                        await _fileMetadataRepository.UpdateAsync(uploadedFile, cancellationToken);
                    }
                    else
                    {
                        await _fileMetadataRepository.AddAsync(uploadedFile, cancellationToken);
                    }

                    completedFiles++;
                    completedBytes += file.Size;
                    ReportProgress(accountId, SyncStatus.Running, totalFiles, completedFiles, totalBytes, completedBytes, filesUploading: 1, conflictsDetected: conflictCount);
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
                    completedFiles++;
                    ReportProgress(accountId, SyncStatus.Running, totalFiles, completedFiles, totalBytes, completedBytes, conflictsDetected: conflictCount);
                }
            }

            // Download files
            for (int i = 0; i < filesToDownload.Count; i++)
            {
                _syncCancellation.Token.ThrowIfCancellationRequested();

                var file = filesToDownload[i];

                try
                {
                    System.Diagnostics.Debug.WriteLine($"Starting download: {file.Name} (ID: {file.Id}) to {file.LocalPath}");

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

                    if (existingRecord is not null)
                    {
                        await _fileMetadataRepository.UpdateAsync(downloadedFile, cancellationToken);
                    }
                    else
                    {
                        await _fileMetadataRepository.AddAsync(downloadedFile, cancellationToken);
                    }

                    System.Diagnostics.Debug.WriteLine($"Successfully synced: {file.Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR downloading {file.Name}: {ex.GetType().Name} - {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

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
                }

                completedFiles++;
                completedBytes += file.Size;
                ReportProgress(accountId, SyncStatus.Running, totalFiles, completedFiles, totalBytes, completedBytes, filesDownloading: 1, conflictsDetected: conflictCount);
            }

            // Handle deletions
            foreach (var fileToDelete in filesToDelete)
            {
                await _fileMetadataRepository.DeleteAsync(fileToDelete.Id, cancellationToken);
            }

            ReportProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes, completedBytes, filesDeleted: filesToDelete.Count, conflictsDetected: conflictCount);

            // Finalize sync session log if detailed logging is enabled
            if (_currentSessionId is not null)
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
                _currentSessionId = null;
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
        catch (Exception)
        {
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
        int conflictsDetected = 0)
    {
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
            MegabytesPerSecond: 0, // Calculate in future
            EstimatedSecondsRemaining: null, // Calculate in future
            LastUpdateUtc: DateTime.UtcNow);

        _progressSubject.OnNext(progress);
    }

    public void Dispose()
    {
        _syncCancellation?.Dispose();
        _progressSubject.Dispose();
    }
}
