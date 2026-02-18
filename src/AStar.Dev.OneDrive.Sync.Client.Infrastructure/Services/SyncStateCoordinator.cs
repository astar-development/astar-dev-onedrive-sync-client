using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Coordinates sync state management and progress reporting.
/// </summary>
public sealed class SyncStateCoordinator : ISyncStateCoordinator, IDisposable
{
    private readonly ISyncSessionLogRepository _syncSessionLogRepository;
    private readonly BehaviorSubject<SyncState> _progressSubject;
    private readonly List<(DateTimeOffset Timestamp, long Bytes)> _transferHistory = [];
    private Guid? _currentSessionId;
    private long _lastCompletedBytes;
    private DateTimeOffset _lastProgressUpdate = DateTimeOffset.UtcNow;

    public SyncStateCoordinator(ISyncSessionLogRepository syncSessionLogRepository)
    {
        _syncSessionLogRepository = syncSessionLogRepository ?? throw new ArgumentNullException(nameof(syncSessionLogRepository));
        var initialState = SyncState.CreateInitial(string.Empty, new HashedAccountId(string.Empty));
        _progressSubject = new BehaviorSubject<SyncState>(initialState);
    }

    public void Dispose() => _progressSubject.Dispose();

    /// <inheritdoc />
    public IObservable<SyncState> Progress => _progressSubject;

    /// <inheritdoc />
    public async Task<Guid> InitializeSessionAsync(HashedAccountId hashedAccountId, bool enableDetailedLogging, CancellationToken cancellationToken = default)
    {
        if(enableDetailedLogging)
        {
            var sessionLog = SyncSessionLog.CreateInitialRunning(hashedAccountId);
            await _syncSessionLogRepository.AddAsync(sessionLog, cancellationToken);
            _currentSessionId = sessionLog.Id;
            return sessionLog.Id;
        }

        _currentSessionId = Guid.CreateVersion7();
        return _currentSessionId.Value;
    }

    /// <inheritdoc />
    public void UpdateProgress(
        string accountId,
        HashedAccountId hashedAccountId,
        SyncStatus status,
        int totalFiles = 0,
        int completedFiles = 0,
        long totalBytes = 0,
        long completedBytes = 0,
        int filesDownloading = 0,
        int filesUploading = 0,
        int filesDeleted = 0,
        int conflictsDetected = 0,
        string? currentScanningFolder = null,
        Guid? sessionId = null,
        long? phaseTotalBytes = null)
    {
        DateTimeOffset now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastProgressUpdate).TotalSeconds;

        double megabytesPerSecond = 0;
        if(elapsedSeconds > 0.1)
        {
            var bytesDelta = completedBytes - _lastCompletedBytes;
            if(bytesDelta > 0)
            {
                var megabytesDelta = bytesDelta / (1024.0 * 1024.0);
                megabytesPerSecond = megabytesDelta / elapsedSeconds;

                _transferHistory.Add((now, completedBytes));
                if(_transferHistory.Count > 10)
                    _transferHistory.RemoveAt(0);

                if(_transferHistory.Count >= 2)
                {
                    var totalElapsed = (now - _transferHistory[0].Timestamp).TotalSeconds;
                    var totalTransferred = completedBytes - _transferHistory[0].Bytes;
                    if(totalElapsed > 0)
                        megabytesPerSecond = totalTransferred / (1024.0 * 1024.0) / totalElapsed;
                }

                _lastProgressUpdate = now;
                _lastCompletedBytes = completedBytes;
            }
        }

        int? estimatedSecondsRemaining = null;
        var bytesForEta = phaseTotalBytes ?? totalBytes;
        if(megabytesPerSecond > 0.01 && completedBytes < bytesForEta)
        {
            var remainingBytes = bytesForEta - completedBytes;
            var remainingMegabytes = remainingBytes / (1024.0 * 1024.0);
            estimatedSecondsRemaining = (int)Math.Ceiling(remainingMegabytes / megabytesPerSecond);
        }

        var progress = new SyncState(
            accountId,
            hashedAccountId,
            status,
            totalFiles,
            completedFiles,
            totalBytes,
            completedBytes,
            filesDownloading,
            filesUploading,
            filesDeleted,
            conflictsDetected,
            megabytesPerSecond,
            estimatedSecondsRemaining,
            currentScanningFolder,
            now);

        _progressSubject.OnNext(progress);
    }

    /// <inheritdoc />
    public async Task RecordCompletionAsync(int uploadCount, int downloadCount, int deleteCount, int conflictCount, long completedBytes, Guid? sessionId = null, CancellationToken cancellationToken = default)
    {
        if(sessionId is null)
            return;

        SyncSessionLog? session = await _syncSessionLogRepository.GetByIdAsync(_currentSessionId!.Value, cancellationToken);
        if(session is not null)
        {
            SyncSessionLog updatedSession = session with
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
    }

    /// <inheritdoc />
    public async Task RecordFailureAsync(CancellationToken cancellationToken = default)
    {
        if(_currentSessionId is null)
            return;

        SyncSessionLog? session = await _syncSessionLogRepository.GetByIdAsync(_currentSessionId!.Value, cancellationToken);
        if(session is not null)
        {
            SyncSessionLog updatedSession = session with { CompletedUtc = DateTime.UtcNow, Status = SyncStatus.Failed };
            await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RecordCancellationAsync(CancellationToken cancellationToken = default)
    {
        if(_currentSessionId is null)
            return;

        SyncSessionLog? session = await _syncSessionLogRepository.GetByIdAsync(_currentSessionId!.Value, cancellationToken);
        if(session is not null)
        {
            SyncSessionLog updatedSession = session with { CompletedUtc = DateTime.UtcNow, Status = SyncStatus.Paused };
            await _syncSessionLogRepository.UpdateAsync(updatedSession, cancellationToken);
        }
    }

    /// <inheritdoc />
    public SyncState GetCurrentState() => _progressSubject.Value;

    /// <inheritdoc />
    public Guid GetCurrentSessionId() => _currentSessionId ?? Guid.CreateVersion7();

    /// <inheritdoc />
    public void ResetTrackingDetails(long completedBytes = 0)
    {
        _transferHistory.Clear();
        _lastProgressUpdate = DateTimeOffset.UtcNow;
        _lastCompletedBytes = completedBytes;
    }
}
