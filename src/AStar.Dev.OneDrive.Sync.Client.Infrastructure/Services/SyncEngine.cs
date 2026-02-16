using System.Text.RegularExpressions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;
using Unit = AStar.Dev.Functional.Extensions.Unit;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for synchronizing files between local storage and OneDrive.
/// </summary>
/// <remarks>
///     Supports bidirectional sync with conflict detection and resolution.
///     Uses LastWriteWins strategy: when both local and remote files change, the newer timestamp wins.
/// </remarks>
public sealed partial class SyncEngine(
    ILocalFileScanner localFileScanner,
    ISyncConfigurationRepository syncConfigurationRepository,
    IAccountRepository accountRepository,
    ISyncConflictRepository syncConflictRepository,
    IConflictDetectionService conflictDetectionService,
    IDeltaProcessingService deltaProcessingService,
    IFileTransferService fileTransferService,
    IDeletionSyncService deletionSyncService,
    ISyncStateCoordinator syncStateCoordinator) : ISyncEngine, IDisposable
{
    private readonly IAccountRepository _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    private readonly IConflictDetectionService _conflictDetectionService = conflictDetectionService ?? throw new ArgumentNullException(nameof(conflictDetectionService));
    private readonly IDeletionSyncService _deletionSyncService = deletionSyncService ?? throw new ArgumentNullException(nameof(deletionSyncService));
    private readonly IDeltaProcessingService _deltaProcessingService = deltaProcessingService ?? throw new ArgumentNullException(nameof(deltaProcessingService));
    private readonly IFileTransferService _fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
    private readonly ILocalFileScanner _localFileScanner = localFileScanner ?? throw new ArgumentNullException(nameof(localFileScanner));
    private readonly ISyncConfigurationRepository _syncConfigurationRepository = syncConfigurationRepository ?? throw new ArgumentNullException(nameof(syncConfigurationRepository));
    private readonly ISyncConflictRepository _syncConflictRepository = syncConflictRepository ?? throw new ArgumentNullException(nameof(syncConflictRepository));
    private readonly ISyncStateCoordinator _syncStateCoordinator = syncStateCoordinator ?? throw new ArgumentNullException(nameof(syncStateCoordinator));
    private CancellationTokenSource? _syncCancellation;
    private int _syncInProgress;

    public void Dispose() => _syncCancellation?.Dispose();

    /// <inheritdoc />
    public IObservable<SyncState> Progress => _syncStateCoordinator.Progress;

    /// <inheritdoc />
    public async Task StartSyncAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.SyncEngine.StartSync, hashedAccountId, cancellationToken);
        DebugLogContext.SetAccountId(hashedAccountId);

        if(SyncIsAlreadyRunning())
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId, $"Sync already in progress for account {hashedAccountId}, ignoring duplicate request. Exiting", cancellationToken);
            return;
        }

        _syncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _syncStateCoordinator.ResetTrackingDetails();

            AccountInfo? account = await ValidateAndGetAccountAsync(hashedAccountId, cancellationToken)
                .MatchAsync(
                    account => account,
                    error =>
                    {
                        _syncStateCoordinator.UpdateProgress(accountId, hashedAccountId, SyncStatus.Failed);
                        return (AccountInfo?)null;
                    });

            if(account is null)
            {
                return;
            }

            _ = await ProcessDeltaChangesAsync(accountId, hashedAccountId, cancellationToken)
                .MatchAsync(
                    async _ => Unit.Value,
                    async error =>
                    {
                        // Log error but continue - delta processing errors shouldn't stop the sync
                        await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", hashedAccountId,
                            $"Delta processing failed: {error.Message}", error.Exception, cancellationToken);
                        return Unit.Value;
                    });

            IReadOnlyList<DriveItemEntity> folders = await GetSelectedFoldersAsync(accountId, cancellationToken);
            if(folders.Count == 0)
            {
                _syncStateCoordinator.UpdateProgress(accountId, hashedAccountId, SyncStatus.Idle);
                return;
            }

            var currentSessionId = await _syncStateCoordinator.InitializeSessionAsync(accountId, hashedAccountId, account.EnableDetailedSyncLogging, cancellationToken);

            List<FileMetadata> allLocalFiles = await ScanLocalFilesAsync(accountId, folders, account);
            var existingFilesDict = folders.ToDictionary(f => f.RelativePath ?? "", f => f);
            var localFilesDict = allLocalFiles.ToDictionary(f => f.RelativePath ?? "", f => f);

            List<FileMetadata> filesToUpload = await DetectFilesToUploadAsync(
                accountId, allLocalFiles, existingFilesDict, folders, cancellationToken);

            var remotePathsSet = folders.Select(f => f.RelativePath).ToHashSet();
            var localPathsSet = allLocalFiles.Select(f => f.RelativePath).ToHashSet();

            (List<FileMetadata> filesToDownload, var conflictCount, HashSet<string> conflictPaths) =
                await DetectFilesToDownloadAndConflictsAsync(
                    hashedAccountId, folders, existingFilesDict, localFilesDict, account, cancellationToken);

            await _deletionSyncService.ProcessRemoteToLocalDeletionsAsync(
                hashedAccountId, folders, remotePathsSet, localPathsSet, cancellationToken);

            await _deletionSyncService.ProcessLocalToRemoteDeletionsAsync(
                accountId, hashedAccountId, allLocalFiles, remotePathsSet, localPathsSet, cancellationToken);

            filesToUpload = FilterUploadsByDeletionsAndConflicts(
                filesToUpload, folders, remotePathsSet, conflictPaths);

            (filesToDownload, var totalFiles, var totalBytes, var uploadBytes, var downloadBytes) =
                await CalculateSyncSummaryAsync(
                    accountId, filesToUpload, filesToDownload, cancellationToken);

            var filesDeleted = 0;
            _syncStateCoordinator.UpdateProgress(accountId, hashedAccountId, SyncStatus.Running, totalFiles, 0, totalBytes,
                filesDeleted: filesDeleted, conflictsDetected: conflictCount);

            var completedFiles = 0;
            var completedBytes = 0L;

            (completedFiles, completedBytes) = await _fileTransferService.ExecuteUploadsAsync(accountId,
                hashedAccountId, folders, filesToUpload, account.MaxParallelUpDownloads,
                conflictCount, totalFiles, totalBytes, uploadBytes, completedFiles, completedBytes, currentSessionId,
                _syncStateCoordinator.UpdateProgress, _syncCancellation, cancellationToken);

            _syncStateCoordinator.ResetTrackingDetails(completedBytes);

            (completedFiles, completedBytes) = await _fileTransferService.ExecuteDownloadsAsync(accountId,
                hashedAccountId, folders, filesToDownload, account.MaxParallelUpDownloads,
                conflictCount, totalFiles, totalBytes, uploadBytes, downloadBytes, completedFiles, completedBytes, currentSessionId,
                _syncStateCoordinator.UpdateProgress, _syncCancellation, cancellationToken);

            _syncStateCoordinator.UpdateProgress(accountId, hashedAccountId, SyncStatus.Completed, totalFiles, completedFiles, totalBytes,
                completedBytes, filesDeleted: filesDeleted, conflictsDetected: conflictCount);

            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId,
                $"Sync completed: {totalFiles} files, {completedBytes} bytes", cancellationToken);
            await DebugLog.ExitAsync("SyncEngine.StartSyncAsync", hashedAccountId, cancellationToken);

            await _syncStateCoordinator.RecordCompletionAsync(filesToUpload.Count,
                filesToDownload.Count, filesDeleted, conflictCount, completedBytes, cancellationToken);

            await UpdateLastAccountSyncAsync(account, cancellationToken);
        }
        catch(OperationCanceledException)
        {
            await _syncStateCoordinator.RecordCancellationAsync(cancellationToken);
            _syncStateCoordinator.UpdateProgress(accountId, hashedAccountId, SyncStatus.Paused);
            throw;
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync("SyncEngine.StartSyncAsync", hashedAccountId,
                $"Sync failed: {ex.Message}", ex, cancellationToken);
            await _syncStateCoordinator.RecordFailureAsync(cancellationToken);
            _syncStateCoordinator.UpdateProgress(accountId, hashedAccountId, SyncStatus.Failed);
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
    internal async Task<Result<AccountInfo, SyncError>> ValidateAndGetAccountAsync(
        HashedAccountId hashedAccountId,
        CancellationToken cancellationToken) => await Try.RunAsync(async () =>
                                                         {
                                                             AccountInfo? account = await _accountRepository.GetByIdAsync(hashedAccountId, cancellationToken);

                                                             return account is null ? throw new InvalidOperationException($"Account '{hashedAccountId}' not found") : account;
                                                         })
            .MapFailureAsync(ex =>
                ex is InvalidOperationException
                    ? SyncError.AccountNotFound(hashedAccountId)
                    : SyncError.SyncFailed($"Failed to retrieve account: {ex.Message}", ex));

    /// <summary>
    ///     Processes delta changes using Result pattern.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="hashedAccountId">The hashed account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    internal async Task<Result<Unit, SyncError>> ProcessDeltaChangesAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken)
        => await Try.RunAsync(async () =>
                    {
                        DeltaToken? token = await _deltaProcessingService.GetDeltaTokenAsync(accountId, cancellationToken);
                        (DeltaToken? finalDelta, var pageCount, var totalItemsProcessed) =
                            await _deltaProcessingService.ProcessDeltaPagesAsync(
                                accountId,
                                hashedAccountId,
                                token,
                                state => _syncStateCoordinator.UpdateProgress(accountId, state.HashedAccountId, state.Status, state.TotalFiles, state.CompletedFiles,
                                    state.TotalBytes, state.CompletedBytes, state.FilesDownloading, state.FilesUploading,
                                    state.FilesDeleted, state.ConflictsDetected, state.CurrentStatusMessage, null),
                                cancellationToken);
                        await _deltaProcessingService.SaveDeltaTokenAsync(finalDelta, cancellationToken);
                        await DebugLog.EntryAsync("SyncEngine.ProcessDeltaChangesAsync", hashedAccountId, cancellationToken);

                        return Unit.Value;
                    })
            .MapFailureAsync(ex => SyncError.DeltaProcessingFailed(ex.Message, ex));

    private async Task<IReadOnlyList<DriveItemEntity>> GetSelectedFoldersAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken)
    {
        IReadOnlyList<DriveItemEntity> folders = await _syncConfigurationRepository
            .GetSelectedItemsByAccountIdAsync(hashedAccountId, cancellationToken);

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId, $"Starting sync with {folders.Count} selected folders: {string.Join(", ", folders)}", cancellationToken);

        return folders;
    }

    private async Task<List<FileMetadata>> ScanLocalFilesAsync(HashedAccountId hashedAccountId, IReadOnlyList<DriveItemEntity> selectedFolders, AccountInfo account)
    {
        var allLocalFiles = new List<FileMetadata>();
        foreach(DriveItemEntity driveItem in selectedFolders.Where(f => f.IsFolder))
        {
            var localFolderPath = Path.Combine(account.LocalSyncPath, driveItem.RelativePath.TrimStart('/'));
            IReadOnlyList<FileMetadata> localFiles = await _localFileScanner.ScanFolderAsync(
                hashedAccountId,
                localFolderPath,
                driveItem.RelativePath,
                _syncCancellation?.Token ?? CancellationToken.None);
            if(localFiles?.Count > 0)
                allLocalFiles.AddRange(localFiles);
        }

        return allLocalFiles.DistinctBy(f => f.RelativePath).ToList();
    }

    private static async Task<List<FileMetadata>> DetectFilesToUploadAsync(HashedAccountId hashedAccountId, List<FileMetadata> allLocalFiles, Dictionary<string, DriveItemEntity> existingFilesDict,
        IReadOnlyList<DriveItemEntity> folders, CancellationToken cancellationToken)
    {
        var filesToUpload = new List<FileMetadata>();

        foreach(FileMetadata localFile in allLocalFiles)
        {
            if(existingFilesDict.TryGetValue(localFile.RelativePath, out DriveItemEntity? existingFile))
            {
                if(existingFile.SyncStatus is FileSyncStatus.PendingUpload or FileSyncStatus.Failed)
                {
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId,
                        $"File needs upload (status={existingFile.SyncStatus}): {localFile.Name}", cancellationToken);
                    var fileToUpload = new FileMetadata(
                        existingFile.DriveItemId,
                        hashedAccountId,
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
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId,
                                $"File marked as changed: {localFile.Name} - Hash changed (DB: {existingFile.LocalHash}, Local: {localFile.LocalHash})",
                                cancellationToken);
                        }
                    }
                    else
                    {
                        hasChanged = existingFile.Size != localFile.Size;
                        if(hasChanged)
                        {
                            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId,
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
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId,
                    $"New local file to upload: {localFile.Name}", cancellationToken);
                filesToUpload.Add(localFile);
            }
        }

        return filesToUpload;
    }

    private async Task<(List<FileMetadata> FilesToDownload, int ConflictCount, HashSet<string> ConflictPaths)> DetectFilesToDownloadAndConflictsAsync(
        HashedAccountId hashedAccountId, IReadOnlyList<DriveItemEntity> folders, Dictionary<string, DriveItemEntity> existingFilesDict, Dictionary<string, FileMetadata> localFilesDict,
        AccountInfo account, CancellationToken cancellationToken)
    {
        var filesToDownload = new List<FileMetadata>();
        var conflictCount = 0;
        var conflictPaths = new HashSet<string>();
        var filesToRecordWithoutTransfer = new List<FileMetadata>();
        var sessionId = _syncStateCoordinator.GetCurrentSessionId();

        foreach(DriveItemEntity remoteFile in folders)
        {
            if(existingFilesDict.TryGetValue(remoteFile.RelativePath ?? "", out DriveItemEntity? existingFile))
            {
                (var HasConflict, FileMetadata? FileToDownload) = await _conflictDetectionService.CheckKnownFileConflictAsync(
                    hashedAccountId, remoteFile, existingFile, localFilesDict, account.LocalSyncPath, sessionId, cancellationToken);
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
                    hashedAccountId, remoteFile, localFilesDict, account.LocalSyncPath, sessionId, cancellationToken);
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
            HashedAccountId hashedAccountId,
            List<FileMetadata> filesToUpload,
            List<FileMetadata> filesToDownload,
            CancellationToken cancellationToken)
    {
        var totalFiles = filesToUpload.Count + filesToDownload.Count;
        var totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
        var uploadBytes = filesToUpload.Sum(f => f.Size);
        var downloadBytes = filesToDownload.Sum(f => f.Size);

        await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId,
            $"Sync summary: {filesToDownload.Count} to download, {filesToUpload.Count} to upload",
            cancellationToken);

        (filesToDownload, totalFiles, totalBytes, downloadBytes) =
            await RemoveDuplicatesFromDownloadList(filesToUpload, filesToDownload, totalFiles, totalBytes, downloadBytes, hashedAccountId, cancellationToken);

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
    public async Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default) => await _syncConflictRepository.GetUnresolvedByAccountIdAsync(hashedAccountId, cancellationToken);

    private static async Task<(List<FileMetadata> filesToDownload, int totalFiles, long totalBytes, long downloadBytes)> RemoveDuplicatesFromDownloadList(List<FileMetadata> filesToUpload,
        List<FileMetadata> filesToDownload, int totalFiles, long totalBytes, long downloadBytes, HashedAccountId hashedAccountId, CancellationToken cancellationToken)
    {
        var duplicateDownloads = filesToDownload.GroupBy(f => f.RelativePath).Where(g => g.Count() > 1).ToList();
        if(duplicateDownloads.Count > 0)
        {
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId, $"WARNING: Found {duplicateDownloads.Count} duplicate paths in download list!", cancellationToken);
            foreach(IGrouping<string, FileMetadata>? dup in duplicateDownloads)
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId, $"  Duplicate: {dup.Key} appears {dup.Count()} times", cancellationToken);

            filesToDownload = [.. filesToDownload.GroupBy(f => f.RelativePath).Select(g => g.First())];
            await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", hashedAccountId, $"After deduplication: {filesToDownload.Count} files to download", cancellationToken);

            totalFiles = filesToUpload.Count + filesToDownload.Count;
            totalBytes = filesToUpload.Sum(f => f.Size) + filesToDownload.Sum(f => f.Size);
            downloadBytes = filesToDownload.Sum(f => f.Size);
        }

        return (filesToDownload, totalFiles, totalBytes, downloadBytes);
    }

    private async Task<List<FileMetadata>> GetAllLocalFiles(HashedAccountId hashedAccountId, IReadOnlyList<DriveItemEntity> selectedFolders, AccountInfo account)
    {
        var allLocalFiles = new List<FileMetadata>();
        foreach(DriveItemEntity driveItem in selectedFolders.Where(f => f.IsFolder))
        {
            var localFolderPath = Path.Combine(account.LocalSyncPath, driveItem.RelativePath.TrimStart('/'));
            IReadOnlyList<FileMetadata> localFiles = await _localFileScanner.ScanFolderAsync(
                hashedAccountId,
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
