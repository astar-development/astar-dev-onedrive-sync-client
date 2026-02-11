using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using AStar.Dev.Functional.Extensions;
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
    private readonly IAccountRepository _accountRepository;
    private readonly IConflictDetectionService _conflictDetectionService;
    private readonly IDeletionSyncService _deletionSyncService;
    private readonly IDeltaProcessingService _deltaProcessingService;
    private readonly IDriveItemsRepository _driveItemsRepository;
    private readonly IFileTransferService _fileTransferService;
    private readonly IGraphApiClient _graphApiClient;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly IRemoteChangeDetector _remoteChangeDetector;
    private readonly ISyncConfigurationRepository _syncConfigurationRepository;
    private readonly ISyncConflictRepository _syncConflictRepository;
    private readonly ISyncStateCoordinator _syncStateCoordinator;
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
        IConflictDetectionService conflictDetectionService,
        IDeltaProcessingService deltaProcessingService,
        IFileTransferService fileTransferService,
        IDeletionSyncService deletionSyncService,
        ISyncStateCoordinator syncStateCoordinator)
    {
        _localFileScanner = localFileScanner ?? throw new ArgumentNullException(nameof(localFileScanner));
        _remoteChangeDetector = remoteChangeDetector ?? throw new ArgumentNullException(nameof(remoteChangeDetector));
        _driveItemsRepository = fileMetadataRepository ?? throw new ArgumentNullException(nameof(fileMetadataRepository));
        _syncConfigurationRepository = syncConfigurationRepository ?? throw new ArgumentNullException(nameof(syncConfigurationRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _syncConflictRepository = syncConflictRepository ?? throw new ArgumentNullException(nameof(syncConflictRepository));
        _conflictDetectionService = conflictDetectionService ?? throw new ArgumentNullException(nameof(conflictDetectionService));
        _deltaProcessingService = deltaProcessingService ?? throw new ArgumentNullException(nameof(deltaProcessingService));
        _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
        _deletionSyncService = deletionSyncService ?? throw new ArgumentNullException(nameof(deletionSyncService));
        _syncStateCoordinator = syncStateCoordinator ?? throw new ArgumentNullException(nameof(syncStateCoordinator));
    }

    public void Dispose()
    {
        _syncCancellation?.Dispose();
    }

    /// <inheritdoc />
    public IObservable<SyncState> Progress => _syncStateCoordinator.Progress;

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
            _syncStateCoordinator.ResetTrackingDetails();

            Result<AccountInfo, SyncError> accountResult = await ValidateAndGetAccountAsync(accountId, cancellationToken);
            AccountInfo? account = accountResult.Match(
                account => account,
                error =>
                {
                    _syncStateCoordinator.UpdateProgress(accountId, SyncStatus.Failed);
                    return (AccountInfo?)null;
                });

            if(account is null)
            {
                return;
            }

            Result<Unit, SyncError> deltaResult = await ProcessDeltaChangesAsync(accountId, cancellationToken);
            deltaResult.Match(
                _ => Unit.Value,
                error =>
                {
                    // Log error but continue - delta processing errors shouldn't stop the sync
                    DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", accountId, 
                        $"Delta processing failed: {error.Message}", error.Exception, cancellationToken).Wait();
                    return Unit.Value;
                });

            IReadOnlyList<DriveItemEntity> folders = await GetSelectedFoldersAsync(accountId, cancellationToken);
            if(folders.Count == 0)
            {
                _syncStateCoordinator.UpdateProgress(accountId, SyncStatus.Idle);
                return;
            }

            string? currentSessionId = await _syncStateCoordinator.InitializeSessionAsync(accountId, account.EnableDetailedSyncLogging, cancellationToken);

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
            _syncStateCoordinator.UpdateProgress(accountId, SyncStatus.Running, totalFiles, 0, totalBytes,
                filesDeleted: filesDeleted, conflictsDetected: conflictCount);

            var completedFiles = 0;
            var completedBytes = 0L;

            (completedFiles, completedBytes) = await _fileTransferService.ExecuteUploadsAsync(
                accountId, folders, filesToUpload, account.MaxParallelUpDownloads,
                conflictCount, totalFiles, totalBytes, uploadBytes, completedFiles, completedBytes, currentSessionId,
                _syncStateCoordinator.UpdateProgress, _syncCancellation, cancellationToken);

            _syncStateCoordinator.ResetTrackingDetails(completedBytes);

            (completedFiles, completedBytes) = await _fileTransferService.ExecuteDownloadsAsync(
                accountId, folders, filesToDownload, account.MaxParallelUpDownloads,
                conflictCount, totalFiles, totalBytes, uploadBytes, downloadBytes, completedFiles, completedBytes, currentSessionId,
                _syncStateCoordinator.UpdateProgress, _syncCancellation, cancellationToken);

            _syncStateCoordinator.UpdateProgress(accountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes,
                completedBytes, filesDeleted: filesDeleted, conflictsDetected: conflictCount);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId,
                $"Sync completed: {totalFiles} files, {completedBytes} bytes", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", accountId, cancellationToken);

            await _syncStateCoordinator.RecordCompletionAsync(filesToUpload.Count,
                filesToDownload.Count, filesDeleted, conflictCount, completedBytes, cancellationToken);
            
            await UpdateLastAccountSyncAsync(account, cancellationToken);
        }
        catch(OperationCanceledException)
        {
            await _syncStateCoordinator.RecordCancellationAsync(cancellationToken);
            _syncStateCoordinator.UpdateProgress(accountId, SyncStatus.Paused);
            throw;
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", accountId,
                $"Sync failed: {ex.Message}", ex, cancellationToken);
            await _syncStateCoordinator.RecordFailureAsync(cancellationToken);
            _syncStateCoordinator.UpdateProgress(accountId, SyncStatus.Failed);
            throw;
        }
        finally
        {
            DebugLogContext.Clear();
            _ = Interlocked.Exchange(ref _syncInProgress, 0);
        }
    }

    /// <summary>
    ///     Validates and retrieves account information using Result pattern.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the account info or an error.</returns>
    public async Task<Result<AccountInfo, SyncError>> ValidateAndGetAccountAsync(
        string accountId, 
        CancellationToken cancellationToken)
    {
        try
        {
            AccountInfo? account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
            
            if (account is null)
            {
                return SyncError.AccountNotFound(accountId);
            }

            return account;
        }
        catch (Exception ex)
        {
            return SyncError.SyncFailed($"Failed to retrieve account: {ex.Message}", ex);
        }
    }



    /// <summary>
    ///     Processes delta changes using Result pattern.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result<Unit, SyncError>> ProcessDeltaChangesAsync(
        string accountId, 
        CancellationToken cancellationToken)
    {
        try
        {
            DeltaToken? token = await _deltaProcessingService.GetDeltaTokenAsync(accountId, cancellationToken);
            (DeltaToken? finalDelta, var pageCount, var totalItemsProcessed) =
                await _deltaProcessingService.ProcessDeltaPagesAsync(
                    accountId,
                    token,
                    state => _syncStateCoordinator.UpdateProgress(state.AccountId, state.Status, state.TotalFiles, state.CompletedFiles,
                        state.TotalBytes, state.CompletedBytes, state.FilesDownloading, state.FilesUploading,
                        state.FilesDeleted, state.ConflictsDetected, state.CurrentStatusMessage, null),
                    cancellationToken);
            await _deltaProcessingService.SaveDeltaTokenAsync(finalDelta, cancellationToken);
            await DebugLog.EntryAsync("SyncEngine.ProcessDeltaChangesAsync", accountId, cancellationToken);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            return SyncError.DeltaProcessingFailed(ex.Message, ex);
        }
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
        string? sessionId = _syncStateCoordinator.GetCurrentSessionId();

        foreach(DriveItemEntity remoteFile in folders)
        {
            if(existingFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out DriveItemEntity? existingFile))
            {
                (var HasConflict, FileMetadata? FileToDownload) = await _conflictDetectionService.CheckKnownFileConflictAsync(
                    accountId, remoteFile, existingFile, localFilesDict, account.LocalSyncPath, sessionId, cancellationToken);
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
                (var HasConflict, FileMetadata? FileToDownload, FileMetadata? MatchedFile) = await _conflictDetectionService.CheckFirstSyncFileConflictAsync(
                    accountId, remoteFile, localFilesDict, account.LocalSyncPath, sessionId, cancellationToken);
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

    private bool SyncIsAlreadyRunning() => Interlocked.CompareExchange(ref _syncInProgress, 1, 0) != 0;

    private async Task UpdateLastAccountSyncAsync(AccountInfo account, CancellationToken cancellationToken)
    {
        AccountInfo lastSyncUpdate = account with { LastSyncUtc = DateTime.UtcNow };

        await _accountRepository.UpdateAsync(lastSyncUpdate, cancellationToken);
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
