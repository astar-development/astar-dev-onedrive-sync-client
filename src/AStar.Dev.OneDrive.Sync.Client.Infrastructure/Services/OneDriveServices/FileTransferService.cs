using System.Diagnostics.CodeAnalysis;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;

[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
public sealed class FileTransferService : IFileTransferService
{
    private const int BatchSize = 50;
    private readonly IGraphApiClient _graphApiClient;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly IDriveItemsRepository _driveItemsRepository;
    private readonly IFileOperationLogRepository _fileOperationLogRepository;

    public FileTransferService(IGraphApiClient graphApiClient, ILocalFileScanner localFileScanner, IDriveItemsRepository driveItemsRepository,
        IFileOperationLogRepository fileOperationLogRepository)
    {
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _localFileScanner = localFileScanner ?? throw new ArgumentNullException(nameof(localFileScanner));
        _driveItemsRepository = driveItemsRepository ?? throw new ArgumentNullException(nameof(driveItemsRepository));
        _fileOperationLogRepository = fileOperationLogRepository ?? throw new ArgumentNullException(nameof(fileOperationLogRepository));
    }

    public async Task<(int CompletedFiles, long CompletedBytes)> ExecuteUploadsAsync(string accountId, IReadOnlyList<DriveItemEntity> existingItems,
        List<FileMetadata> filesToUpload, int maxParallelUploads, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, int completedFiles,
        long completedBytes, string? sessionId, Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter,
        CancellationTokenSource cancellationSource, CancellationToken cancellationToken)
    {
        var maxParallel = Math.Max(1, maxParallelUploads);
        using var uploadSemaphore = new SemaphoreSlim(maxParallel, maxParallel);
        var activeUploads = 0;

        (activeUploads, completedBytes, completedFiles, List<Task> uploadTasks) =
            CreateUploadTasks(accountId, existingItems, filesToUpload, conflictCount, totalFiles, totalBytes, uploadBytes, completedFiles, completedBytes,
                sessionId, progressReporter, uploadSemaphore, activeUploads, cancellationSource, cancellationToken);

        await Task.WhenAll(uploadTasks);

        return (completedFiles, completedBytes);
    }

    public async Task<(int CompletedFiles, long CompletedBytes)> ExecuteDownloadsAsync(string accountId, IReadOnlyList<DriveItemEntity> existingItems,
        List<FileMetadata> filesToDownload, int maxParallelDownloads, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, long downloadBytes,
        int completedFiles, long completedBytes, string? sessionId, Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter,
        CancellationTokenSource cancellationSource, CancellationToken cancellationToken)
    {
        var maxParallel = Math.Max(1, maxParallelDownloads);
        using var downloadSemaphore = new SemaphoreSlim(maxParallel, maxParallel);
        var activeDownloads = 0;

        (activeDownloads, completedBytes, completedFiles, List<Task> downloadTasks) =
            CreateDownloadTasks(accountId, existingItems, filesToDownload, conflictCount, totalFiles, totalBytes, uploadBytes, downloadBytes, completedFiles,
                completedBytes, sessionId, progressReporter, downloadSemaphore, activeDownloads, cancellationSource, cancellationToken);

        await Task.WhenAll(downloadTasks);

        return (completedFiles, completedBytes);
    }

    private (int activeUploads, long completedBytes, int completedFiles, List<Task> uploadTasks) CreateUploadTasks(string accountId, IReadOnlyList<DriveItemEntity> existingItems,
        List<FileMetadata> filesToUpload, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, int completedFiles, long completedBytes, string? sessionId,
        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter, SemaphoreSlim uploadSemaphore, int activeUploads,
        CancellationTokenSource cancellationSource, CancellationToken cancellationToken)
    {
        var batch = new List<FileMetadata>(BatchSize);
        var uploadTasks = filesToUpload.Select(async file =>
        {
            await uploadSemaphore.WaitAsync(cancellationSource.Token);
            _ = Interlocked.Increment(ref activeUploads);

            try
            {
                cancellationSource.Token.ThrowIfCancellationRequested();

                DriveItemEntity? existingFile = existingItems.FirstOrDefault(ie => ie.RelativePath == file.RelativePath && (ie.SyncStatus != FileSyncStatus.Failed || ie.SyncStatus == FileSyncStatus.PendingUpload));
                var isExistingFile = existingFile is not null;

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Uploading {file.Name}: Path={file.RelativePath}, IsExisting={isExistingFile}, LocalPath={file.LocalPath}", cancellationToken);

                if(!isExistingFile)
                {
                    FileMetadata pendingFile = file with { SyncStatus = FileSyncStatus.PendingUpload };
                    await _driveItemsRepository.AddAsync(pendingFile, cancellationToken);
                    await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Added pending upload record to database: {file.Name}", cancellationToken);
                }

                if(sessionId is not null)
                {
                    var reason = isExistingFile ? "File changed locally" : "New file";
                    var operationLog = FileOperationLog.CreateUploadLog(sessionId, accountId, file.RelativePath, file.LocalPath, existingFile?.DriveItemId,
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
                    progressReporter(accountId, SyncStatus.Running, totalFiles, currentCompleted, totalBytes, currentCompletedBytes, 0, currentActiveUploads, 0, conflictCount, null, uploadBytes);
                });

                DriveItem uploadedItem = await _graphApiClient.UploadFileAsync(accountId, file.LocalPath, file.RelativePath, uploadProgress, cancellationSource.Token);

                if(uploadedItem.LastModifiedDateTime.HasValue && File.Exists(file.LocalPath))
                {
                    File.SetLastWriteTimeUtc(file.LocalPath, uploadedItem.LastModifiedDateTime.Value.UtcDateTime);
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
                await SaveBatchIfNeededAsync(batch, BatchSize, cancellationToken);

                _ = Interlocked.Increment(ref completedFiles);
                _ = Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                var finalActiveUploads = Interlocked.CompareExchange(ref activeUploads, 0, 0);
                progressReporter(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, 0, finalActiveUploads, 0, conflictCount, null, uploadBytes);
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

                await SaveBatchIfNeededAsync(batch, BatchSize, cancellationToken);

                _ = Interlocked.Increment(ref completedFiles);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                progressReporter(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, 0, 0, 0, conflictCount, null, uploadBytes);
            }
            finally
            {
                _ = Interlocked.Decrement(ref activeUploads);
                _ = uploadSemaphore.Release();
            }
        }).ToList();

        SaveRemainingBatch(batch);

        return (activeUploads, completedBytes, completedFiles, uploadTasks);
    }

    private (int activeDownloads, long completedBytes, int completedFiles, List<Task> downloadTasks) CreateDownloadTasks(string accountId, IReadOnlyList<DriveItemEntity> existingItems,
        List<FileMetadata> filesToDownload, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, long downloadBytes, int completedFiles, long completedBytes,
        string? sessionId, Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter, SemaphoreSlim downloadSemaphore,
        int activeDownloads, CancellationTokenSource cancellationSource, CancellationToken cancellationToken)
    {
        var batch = new List<FileMetadata>(BatchSize);
        var downloadTasks = filesToDownload.Select(async file =>
        {
            await downloadSemaphore.WaitAsync(cancellationSource.Token);
            _ = Interlocked.Increment(ref activeDownloads);

            try
            {
                cancellationSource.Token.ThrowIfCancellationRequested();

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Starting download: {file.Name} (ID: {file.DriveItemId}) to {file.LocalPath}", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.DownloadFile", accountId, $"Starting download: {file.Name} (ID: {file.DriveItemId}) to {file.LocalPath}", cancellationSource.Token);

                var directory = Path.GetDirectoryName(file.LocalPath);
                if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    await DebugLog.InfoAsync("SyncEngine.DownloadFile", accountId, $"Creating directory: {directory}", cancellationSource.Token);
                    _ = Directory.CreateDirectory(directory);
                }

                if(sessionId is not null)
                {
                    DriveItemEntity? existingFile = existingItems.FirstOrDefault(ie => ie.RelativePath == file.RelativePath && (ie.SyncStatus != FileSyncStatus.Failed || ie.SyncStatus == FileSyncStatus.PendingUpload));
                    var isExistingFile = existingFile is not null;
                    var reason = isExistingFile ? "Remote file changed" : "New remote file";
                    var operationLog = FileOperationLog.CreateDownloadLog(sessionId, accountId, file.RelativePath, file.LocalPath, file.DriveItemId, existingFile?.LocalHash,
                        file.Size, file.LastModifiedUtc, reason);
                    await _fileOperationLogRepository.AddAsync(operationLog, cancellationToken);
                    await _driveItemsRepository.SaveBatchAsync([file with { SyncStatus = FileSyncStatus.PendingDownload }], cancellationToken);
                }

                await _graphApiClient.DownloadFileAsync(accountId, file.DriveItemId, file.LocalPath, cancellationSource.Token);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Download complete: {file.Name}, computing hash...", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.DownloadFile", accountId, $"Download complete: {file.Name}, computing hash...", cancellationSource.Token);

                var downloadedHash = await _localFileScanner.ComputeFileHashAsync(file.LocalPath, cancellationSource.Token);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Hash computed for {file.Name}: {downloadedHash}", cancellationToken);

                FileMetadata downloadedFile = file with { SyncStatus = FileSyncStatus.Synced, LastSyncDirection = SyncDirection.Download, LocalHash = downloadedHash };

                batch.Add(downloadedFile);
                await SaveBatchIfNeededAsync(batch, BatchSize, cancellationToken);

                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Successfully synced: {file.Name}", cancellationToken);

                _ = Interlocked.Increment(ref completedFiles);
                _ = Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                var finalActiveDownloads = Interlocked.CompareExchange(ref activeDownloads, 0, 0);
                progressReporter(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, finalActiveDownloads, 0, 0, conflictCount, null, uploadBytes + downloadBytes);
            }
            catch(Exception ex)
            {
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"ERROR downloading {file.Name}: {ex.GetType().Name} - {ex.Message}", cancellationToken);
                await DebugLog.InfoAsync("SyncEngine.StartSyncAsync", accountId, $"Stack trace: {ex.StackTrace}", cancellationToken);
                await DebugLog.ErrorAsync("SyncEngine.DownloadFile", accountId, $"ERROR downloading {file.Name}: {ex.Message}", ex, cancellationSource.Token);

                FileMetadata failedFile = file with { SyncStatus = FileSyncStatus.Failed };
                batch.Add(failedFile);
                await SaveBatchIfNeededAsync(batch, BatchSize, cancellationToken);

                _ = Interlocked.Increment(ref completedFiles);
                _ = Interlocked.Add(ref completedBytes, file.Size);
                var finalCompleted = Interlocked.CompareExchange(ref completedFiles, 0, 0);
                var finalBytes = Interlocked.Read(ref completedBytes);
                progressReporter(accountId, SyncStatus.Running, totalFiles, finalCompleted, totalBytes, finalBytes, 0, 0, 0, conflictCount, null, uploadBytes + downloadBytes);
            }
            finally
            {
                _ = Interlocked.Decrement(ref activeDownloads);
                _ = downloadSemaphore.Release();
            }
        }).ToList();

        SaveRemainingBatch(batch);

        return (activeDownloads, completedBytes, completedFiles, downloadTasks);
    }

    private async Task SaveBatchIfNeededAsync(List<FileMetadata> batch, int batchSize, CancellationToken cancellationToken)
    {
        if(batch.Count >= batchSize)
        {
            await _driveItemsRepository.SaveBatchAsync(batch, cancellationToken);
            batch.Clear();
        }
    }

    private void SaveRemainingBatch(List<FileMetadata> batch)
    {
        if(batch.Count > 0)
        {
            _driveItemsRepository.SaveBatchAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
            batch.Clear();
        }
    }
}
