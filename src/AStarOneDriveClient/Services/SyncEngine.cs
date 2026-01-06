using System.Reactive.Subjects;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;

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
    private readonly BehaviorSubject<SyncState> _progressSubject;
    private CancellationTokenSource? _syncCancellation;

    public SyncEngine(
        ILocalFileScanner localFileScanner,
        IRemoteChangeDetector remoteChangeDetector,
        IFileMetadataRepository fileMetadataRepository,
        ISyncConfigurationRepository syncConfigurationRepository,
        IAccountRepository accountRepository)
    {
        ArgumentNullException.ThrowIfNull(localFileScanner);
        ArgumentNullException.ThrowIfNull(remoteChangeDetector);
        ArgumentNullException.ThrowIfNull(fileMetadataRepository);
        ArgumentNullException.ThrowIfNull(syncConfigurationRepository);
        ArgumentNullException.ThrowIfNull(accountRepository);

        _localFileScanner = localFileScanner;
        _remoteChangeDetector = remoteChangeDetector;
        _fileMetadataRepository = fileMetadataRepository;
        _syncConfigurationRepository = syncConfigurationRepository;
        _accountRepository = accountRepository;

        var initialState = new SyncState(
            AccountId: string.Empty,
            Status: SyncStatus.Idle,
            TotalFiles: 0,
            CompletedFiles: 0,
            TotalBytes: 0,
            CompletedBytes: 0,
            FilesDownloading: 0,
            FilesUploading: 0,
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
                    if (existingFile.LocalHash != localFile.LocalHash ||
                        existingFile.LastModifiedUtc != localFile.LastModifiedUtc ||
                        existingFile.Size != localFile.Size)
                    {
                        filesToUpload.Add(localFile);
                    }
                }
                else
                {
                    // New file
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
                    if (existingFile.CTag != remoteFile.CTag ||
                        existingFile.LastModifiedUtc != remoteFile.LastModifiedUtc ||
                        existingFile.Size != remoteFile.Size)
                    {
                        filesToDownload.Add(remoteFile);
                    }
                }
                else
                {
                    // New remote file
                    filesToDownload.Add(remoteFile);
                }
            }

            // Detect deletions: files in database but not in remote or local
            var localPathsSet = allLocalFiles.Select(f => f.Path).ToHashSet();
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

            var totalFiles = filesToUpload.Count + filesToDownload.Count;
            var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            ReportProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes, 0, conflictsDetected: conflictCount);

            int completedFiles = 0;
            long completedBytes = 0;

            // Upload files (placeholder - actual upload implementation in future)
            // For Step 6.5, we'll simulate upload and update database
            for (int i = 0; i < filesToUpload.Count; i++)
            {
                _syncCancellation.Token.ThrowIfCancellationRequested();

                var file = filesToUpload[i];

                // Simulate upload (actual Graph API upload will be added later)
                await Task.Delay(10, _syncCancellation.Token); // Simulate network delay

                // Update file metadata with uploaded status
                var uploadedFile = file with
                {
                    Id = $"uploaded_{Guid.NewGuid():N}", // Simulated OneDrive ID
                    SyncStatus = FileSyncStatus.Synced,
                    LastSyncDirection = SyncDirection.Upload
                };

                // Save to database
                if (existingFilesDict.ContainsKey(file.Path))
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

            // Download files (placeholder - actual download implementation in future)
            for (int i = 0; i < filesToDownload.Count; i++)
            {
                _syncCancellation.Token.ThrowIfCancellationRequested();

                var file = filesToDownload[i];

                // Simulate download (actual Graph API download will be added later)
                await Task.Delay(10, _syncCancellation.Token); // Simulate network delay

                // Update file metadata with downloaded status
                var downloadedFile = file with
                {
                    SyncStatus = FileSyncStatus.Synced,
                    LastSyncDirection = SyncDirection.Download,
                    LocalHash = $"downloaded_hash_{Guid.NewGuid():N}" // Simulated hash from downloaded file
                };

                // Save to database
                if (existingFilesDict.ContainsKey(file.Path))
                {
                    await _fileMetadataRepository.UpdateAsync(downloadedFile, cancellationToken);
                }
                else
                {
                    await _fileMetadataRepository.AddAsync(downloadedFile, cancellationToken);
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

            ReportProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes, completedBytes, conflictsDetected: conflictCount);
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
