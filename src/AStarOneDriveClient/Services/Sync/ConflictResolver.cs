using System.Globalization;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services.OneDriveServices;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates - Using IsEnabled checks for performance

namespace AStarOneDriveClient.Services.Sync;

/// <summary>
/// Implements conflict resolution strategies for sync conflicts.
/// </summary>
public sealed class ConflictResolver : IConflictResolver
{
    private readonly IGraphApiClient _graphApiClient;
    private readonly IFileMetadataRepository _metadataRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(
        IGraphApiClient graphApiClient,
        IFileMetadataRepository metadataRepo,
        IAccountRepository accountRepo,
        ILogger<ConflictResolver> logger)
    {
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _metadataRepo = metadataRepo ?? throw new ArgumentNullException(nameof(metadataRepo));
        _accountRepo = accountRepo ?? throw new ArgumentNullException(nameof(accountRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ResolveAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Resolving conflict for file {FilePath} in account {AccountId} with strategy {Strategy}",
                conflict.FilePath,
                conflict.AccountId,
                strategy);
        }

        AccountInfo? account = await _accountRepo.GetByIdAsync(conflict.AccountId, cancellationToken);
        if (account is null)
        {
            throw new InvalidOperationException($"Account not found: {conflict.AccountId}");
        }

        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        switch (strategy)
        {
            case ConflictResolutionStrategy.KeepLocal:
                await KeepLocalVersionAsync(account, conflict, localPath, cancellationToken);
                break;

            case ConflictResolutionStrategy.KeepRemote:
                await KeepRemoteVersionAsync(account, conflict, localPath, cancellationToken);
                break;

            case ConflictResolutionStrategy.KeepBoth:
                await KeepBothVersionsAsync(account, conflict, localPath, cancellationToken);
                break;

            case ConflictResolutionStrategy.None:
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Skipping conflict resolution for {FilePath}", conflict.FilePath);
                }
                break;

            default:
                throw new ArgumentException($"Invalid resolution strategy: {strategy}", nameof(strategy));
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Successfully resolved conflict for {FilePath} with strategy {Strategy}",
                conflict.FilePath,
                strategy);
        }
    }

    private async Task KeepLocalVersionAsync(
        AccountInfo account,
        SyncConflict conflict,
        string localPath,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Keeping local version of {FilePath}", conflict.FilePath);
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Local file not found: {localPath}");
        }

        // Get file metadata to retrieve OneDrive file ID
        FileMetadata? metadata = await _metadataRepo.GetByPathAsync(
            account.AccountId,
            conflict.FilePath,
            cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException($"File metadata not found for {conflict.FilePath}");
        }

        // Upload local file to OneDrive (overwrite remote)
        await _graphApiClient.UploadFileAsync(
            account.AccountId,
            localPath,
            conflict.FilePath,
            cancellationToken);

        // Update metadata
        var fileInfo = new FileInfo(localPath);
        FileMetadata updatedMetadata = metadata with
        {
            Size = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            SyncStatus = FileSyncStatus.Synced,
            LastSyncDirection = SyncDirection.Upload
        };
        await _metadataRepo.UpdateAsync(updatedMetadata, cancellationToken);
    }

    private async Task KeepRemoteVersionAsync(
        AccountInfo account,
        SyncConflict conflict,
        string localPath,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Keeping remote version of {FilePath}", conflict.FilePath);
        }

        // Get file metadata to retrieve OneDrive file ID
        FileMetadata? metadata = await _metadataRepo.GetByPathAsync(
            account.AccountId,
            conflict.FilePath,
            cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException($"File metadata not found for {conflict.FilePath}");
        }

        var fileId = metadata.Id;

        // Download remote file from OneDrive (overwrite local)
        var localDirectory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(localDirectory))
        {
            Directory.CreateDirectory(localDirectory);
        }

        await _graphApiClient.DownloadFileAsync(
            account.AccountId,
            fileId,
            localPath,
            cancellationToken);

        // Update metadata
        var fileInfo = new FileInfo(localPath);
        FileMetadata updatedMetadata = metadata with
        {
            Size = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            SyncStatus = FileSyncStatus.Synced,
            LastSyncDirection = SyncDirection.Download
        };
        await _metadataRepo.UpdateAsync(updatedMetadata, cancellationToken);
    }

    private async Task KeepBothVersionsAsync(
        AccountInfo account,
        SyncConflict conflict,
        string localPath,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Keeping both versions of {FilePath}", conflict.FilePath);
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Local file not found: {localPath}");
        }

        // Get file metadata to retrieve OneDrive file ID
        FileMetadata? metadata = await _metadataRepo.GetByPathAsync(
            account.AccountId,
            conflict.FilePath,
            cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException($"File metadata not found for {conflict.FilePath}");
        }

        var fileId = metadata.Id;

        // Rename local file with conflict suffix
        var directory = Path.GetDirectoryName(localPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(localPath);
        var extension = Path.GetExtension(localPath);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
        var conflictFileName = $"{fileNameWithoutExtension} (Conflict {timestamp}){extension}";
        var conflictPath = Path.Combine(directory, conflictFileName);

        File.Move(localPath, conflictPath);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Renamed local file to {ConflictPath}", conflictPath);
        }

        // Download remote version to original path
        var localDirectory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(localDirectory))
        {
            Directory.CreateDirectory(localDirectory);
        }

        await _graphApiClient.DownloadFileAsync(
            account.AccountId,
            fileId,
            localPath,
            cancellationToken);

        // Update metadata for remote version
        var fileInfo = new FileInfo(localPath);
        FileMetadata updatedMetadata = metadata with
        {
            Size = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            SyncStatus = FileSyncStatus.Synced,
            LastSyncDirection = SyncDirection.Download
        };
        await _metadataRepo.UpdateAsync(updatedMetadata, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Successfully kept both versions: {LocalPath} (remote) and {ConflictPath} (local)",
                localPath,
                conflictPath);
        }
    }
}
