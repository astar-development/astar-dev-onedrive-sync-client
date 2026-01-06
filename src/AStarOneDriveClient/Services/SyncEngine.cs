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
    private readonly BehaviorSubject<SyncState> _progressSubject;
    private CancellationTokenSource? _syncCancellation;

    public SyncEngine(
        ILocalFileScanner localFileScanner,
        IRemoteChangeDetector remoteChangeDetector,
        IFileMetadataRepository fileMetadataRepository,
        ISyncConfigurationRepository syncConfigurationRepository,
        IAccountRepository accountRepository,
        IGraphApiClient graphApiClient)
    {
        ArgumentNullException.ThrowIfNull(localFileScanner);
        ArgumentNullException.ThrowIfNull(remoteChangeDetector);
        ArgumentNullException.ThrowIfNull(fileMetadataRepository);
        ArgumentNullException.ThrowIfNull(syncConfigurationRepository);
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(graphApiClient);

        _localFileScanner = localFileScanner;
        _remoteChangeDetector = remoteChangeDetector;
        _fileMetadataRepository = fileMetadataRepository;
        _syncConfigurationRepository = syncConfigurationRepository;
        _accountRepository = accountRepository;
        _graphApiClient = graphApiClient;

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

            // Determine which files need uploading
            var filesToUpload = new List<FileMetadata>();
            foreach (var localFile in allLocalFiles)
            {
                if (existingFilesDict.TryGetValue(localFile.Path, out var existingFile))
                {
                    // Check if file has changed
                    // Priority: Hash comparison is most reliable when available
                    var bothHaveHashes = !string.IsNullOrEmpty(existingFile.LocalHash) &&
                                        !string.IsNullOrEmpty(localFile.LocalHash);

                    bool hasChanged;
                    if (bothHaveHashes)
                    {
                        // If both files have hashes, trust hash comparison only
                        hasChanged = existingFile.LocalHash != localFile.LocalHash;

                        if (hasChanged)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SyncEngine] File marked as changed: {localFile.Name}");
                            System.Diagnostics.Debug.WriteLine($"  Hash changed (DB: {existingFile.LocalHash}, Local: {localFile.LocalHash})");
                        }
                    }
                    else
                    {
                        // Fallback: use size comparison when hashes unavailable
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
                else
                {
                    // New file
                    System.Diagnostics.Debug.WriteLine($"[SyncEngine] New file to upload: {localFile.Name}");
                    filesToUpload.Add(localFile);
                }
            }

            // Detect remote changes in selected folders
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

            // Determine which files need downloading
            var filesToDownload = new List<FileMetadata>();
            var remotePathsSet = allRemoteFiles.Select(f => f.Path).ToHashSet();

            foreach (var remoteFile in allRemoteFiles)
            {
                if (existingFilesDict.TryGetValue(remoteFile.Path, out var existingFile))
                {
                    // Check if remote file has changed (compare with OneDrive metadata)
                    // CTag is the primary change indicator; also check timestamp with tolerance and size
                    var timeDiff = Math.Abs((existingFile.LastModifiedUtc - remoteFile.LastModifiedUtc).TotalSeconds);
                    var hasChanged = existingFile.CTag != remoteFile.CTag ||
                                   timeDiff > 1.0 ||
                                   existingFile.Size != remoteFile.Size;

                    if (hasChanged)
                    {
                        // Set local path before adding to download list
                        var localFilePath = System.IO.Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                        var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                        filesToDownload.Add(fileWithLocalPath);
                    }
                }
                else
                {
                    // New remote file - set local path
                    var localFilePath = System.IO.Path.Combine(account.LocalSyncPath, remoteFile.Path.TrimStart('/'));
                    var fileWithLocalPath = remoteFile with { LocalPath = localFilePath };
                    filesToDownload.Add(fileWithLocalPath);
                }
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
            var conflictPaths = filesToDownload
                .Where(f => uploadPathsSet.Contains(f.Path))
                .Select(f => f.Path)
                .ToHashSet();

            int conflictCount = conflictPaths.Count;

            // Resolve conflicts using LastWriteWins strategy
            if (conflictCount > 0)
            {
                var resolvedUploads = new List<FileMetadata>();
                var resolvedDownloads = new List<FileMetadata>();

                foreach (var uploadFile in filesToUpload)
                {
                    if (conflictPaths.Contains(uploadFile.Path))
                    {
                        // Find the corresponding remote file
                        var remoteFile = filesToDownload.First(f => f.Path == uploadFile.Path);

                        // LastWriteWins: compare timestamps
                        if (uploadFile.LastModifiedUtc > remoteFile.LastModifiedUtc)
                        {
                            // Local is newer - keep in upload list
                            resolvedUploads.Add(uploadFile);
                        }
                        else
                        {
                            // Remote is newer or equal - add to download list
                            resolvedDownloads.Add(remoteFile);
                        }
                    }
                    else
                    {
                        // No conflict - keep in upload list
                        resolvedUploads.Add(uploadFile);
                    }
                }

                // Keep non-conflicted downloads
                foreach (var downloadFile in filesToDownload)
                {
                    if (!conflictPaths.Contains(downloadFile.Path))
                    {
                        resolvedDownloads.Add(downloadFile);
                    }
                }

                filesToUpload = resolvedUploads;
                filesToDownload = resolvedDownloads;
            }

            // Remove files from upload list that were deleted from OneDrive (since they were deleted locally)
            var deletedPaths = deletedFromOneDrive.Select(f => f.Path).ToHashSet();
            filesToUpload = filesToUpload.Where(f => !deletedPaths.Contains(f.Path)).ToList();

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
                            LastModifiedUtc = uploadedItem.LastModifiedDateTime?.UtcDateTime ?? file.LastModifiedUtc,
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
                            LastModifiedUtc = uploadedItem.LastModifiedDateTime?.UtcDateTime ?? file.LastModifiedUtc,
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

                    var isExistingFile = existingFilesDict.ContainsKey(file.Path);
                    if (isExistingFile)
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

                    // Save to database - check if record exists by ID (not Path)
                    var existingRecord = await _fileMetadataRepository.GetByIdAsync(downloadedFile.Id, cancellationToken);
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
                    if (existingFilesDict.ContainsKey(file.Path))
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
        }
        catch (OperationCanceledException)
        {
            ReportProgress(accountId, SyncStatus.Paused, 0, 0, 0, 0);
            throw;
        }
        catch (Exception)
        {
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
