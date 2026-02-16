using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;

/// <summary>
///     Service responsible for executing file upload and download operations during sync.
/// </summary>
public interface IFileTransferService
{
    /// <summary>
    ///     Executes file uploads to OneDrive with parallel processing and progress reporting.
    /// </summary>
    Task<(int CompletedFiles, long CompletedBytes)> ExecuteUploadsAsync(string accountId, HashedAccountId hashedAccountId, IReadOnlyList<DriveItemEntity> existingItems, List<FileMetadata> filesToUpload,
        int maxParallelUploads, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, int completedFiles, long completedBytes, string? sessionId,
        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter, CancellationTokenSource cancellationSource,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Executes file downloads from OneDrive with parallel processing and progress reporting.
    /// </summary>
    Task<(int CompletedFiles, long CompletedBytes)> ExecuteDownloadsAsync(string accountId, HashedAccountId hashedAccountId, IReadOnlyList<DriveItemEntity> existingItems, List<FileMetadata> filesToDownload,
        int maxParallelDownloads, int conflictCount, int totalFiles, long totalBytes, long uploadBytes, long downloadBytes, int completedFiles, long completedBytes, string? sessionId,
        Action<string, SyncStatus, int, int, long, long, int, int, int, int, string?, long?> progressReporter, CancellationTokenSource cancellationSource,
        CancellationToken cancellationToken);
}
