# Bidirectional Sync Algorithm - Technical Overview

**Project**: AStar Dev - OneDrive Client V3
**Date**: January 5, 2026
**Related**: [Implementation Plan](./multi-account-ux-implementation-plan.md)

---

## Overview

This document provides a technical overview of the bidirectional synchronization algorithm that keeps local files in sync with Microsoft OneDrive using the Graph API Delta Query approach.

### Key Characteristics

- **Bidirectional**: Syncs changes in both directions (OneDrive ↔ Local)
- **Delta-based**: Only fetches/processes changes since last sync (efficient)
- **Conflict-aware**: Detects when both sides change and prompts user
- **Resumable**: Can pause and resume, even across app restarts
- **Change-tracked**: Uses OneDrive's cTag for remote, file watcher for local

---

## Architecture Components

``` text
┌─────────────────────────────────────────────────────────┐
│                   DeltaSyncEngine                       │
│  (Orchestrates bidirectional sync)                      │
└───────────┬─────────────────────────────────┬───────────┘
            │                                 │
┌───────────▼───────────┐         ┌───────────▼───────────┐
│  Remote Change        │         │  Local Change         │
│  Processor            │         │  Processor            │
│  (Download Phase)     │         │  (Upload Phase)       │
└───────────┬───────────┘         └───────────┬───────────┘
            │                                 │
            │         ┌───────────────────┐   │
            └────────►│ Conflict Detector │◄──┘
                      │  (cTag + mtime)   │
                      └─────────┬─────────┘
                                │
                      ┌─────────▼─────────┐
                      │ User Resolution   │
                      │  (if conflict)    │
                      └───────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| **DeltaSyncEngine** | Main orchestrator, manages sync lifecycle |
| **FileWatcherService** | Detects local file system changes in real-time |
| **OneDriveApiService** | Communicates with Microsoft Graph API |
| **FileMetadataRepository** | Tracks synced files in SQLite database |
| **ConflictResolver** | Applies user's conflict resolution strategy |
| **SyncStateRepository** | Persists progress for pause/resume |

---

## Sync Algorithm Flow

### High-Level Process

``` text
1. START SYNC
   ├─► Phase 1: Process Remote Changes (OneDrive → Local)
   │   ├─► Fetch delta changes from Graph API
   │   ├─► For each remote change:
   │   │   ├─► Check if local file also changed (conflict detection)
   │   │   ├─► If conflict: Record and skip
   │   │   └─► If no conflict: Download/delete file
   │   └─► Save new delta token
   │
   ├─► Phase 2: Process Local Changes (Local → OneDrive)
   │   ├─► Query pending uploads from database
   │   ├─► For each local change:
   │   │   ├─► Upload file to OneDrive
   │   │   └─► Update metadata with new cTag
   │   └─► Mark as synced
   │
   └─► Update sync state and notify UI
```

### Detailed Algorithm

```csharp
async Task PerformBidirectionalSync(AccountInfo account)
{
    // PHASE 1: Remote Changes (Download)
    var deltaToken = account.DeltaToken;
    var remoteChanges = await FetchDeltaChanges(account.AccountId, deltaToken);
    
    foreach (var remoteChange in remoteChanges)
    {
        var localMetadata = await GetLocalMetadata(remoteChange.Path);
        
        // Conflict Detection
        if (BothSidesChanged(localMetadata, remoteChange))
        {
            await RecordConflict(remoteChange, localMetadata);
            continue; // Skip until user resolves
        }
        
        // No conflict - apply remote change
        if (remoteChange.Deleted)
            await DeleteLocalFile(remoteChange.Path);
        else
            await DownloadFile(remoteChange);
        
        // Update metadata
        await SaveMetadata(new FileMetadata
        {
            Path = remoteChange.Path,
            CTag = remoteChange.CTag,
            LastModifiedUtc = remoteChange.LastModifiedUtc,
            Size = remoteChange.Size,
            SyncStatus = SyncStatus.Synced,
            LastSyncDirection = SyncDirection.Download
        });
    }
    
    // Save delta token for next sync
    await UpdateDeltaToken(account.AccountId, remoteChanges.NewDeltaToken);
    
    // PHASE 2: Local Changes (Upload)
    var localChanges = await GetPendingUploads(account.AccountId);
    
    foreach (var localChange in localChanges)
    {
        var uploadResult = await UploadFile(localChange);
        
        await SaveMetadata(localChange with
        {
            CTag = uploadResult.CTag,
            ETag = uploadResult.ETag,
            SyncStatus = SyncStatus.Synced,
            LastSyncDirection = SyncDirection.Upload
        });
    }
}
```

---

## Change Detection Strategies

### Remote Changes (OneDrive → Local)

**Method**: Microsoft Graph API Delta Query

```http
GET https://graph.microsoft.com/v1.0/me/drive/root/delta?token={deltaToken}
```

**Response**:

- `value[]`: Array of changed items (files/folders)
- `@odata.deltaLink`: URL with new delta token for next query

**Delta Token**:

- First sync: No token (fetches all items)
- Subsequent syncs: Use saved token (only returns changes)
- Persisted in database for resume capability

**Advantages**:

- ✅ Efficient: Only fetches changes, not entire file tree
- ✅ Server-side: OneDrive tracks changes, no client scanning
- ✅ Comprehensive: Captures all change types (add, modify, delete, rename)

### Local Changes (Local → OneDrive)

**Method**: FileSystemWatcher + Database Tracking

```csharp
var watcher = new FileSystemWatcher(syncDirectory)
{
    IncludeSubdirectories = true,
    NotifyFilter = NotifyFilters.FileName 
                 | NotifyFilters.Size 
                 | NotifyFilters.LastWrite
};

watcher.Changed += OnFileChanged;
watcher.Created += OnFileCreated;
watcher.Deleted += OnFileDeleted;
watcher.Renamed += OnFileRenamed;
```

**Change Processing**:

1. File watcher detects change event
2. Debounce 500ms to avoid partial writes
3. Mark file as `PendingUpload` in database
4. Next sync processes all pending uploads

**Advantages**:

- ✅ Real-time: Detects changes immediately
- ✅ Reliable: OS-level file system notifications
- ✅ Efficient: No polling required

---

## Conflict Detection

### Definition

A **conflict** occurs when:

- Remote file changed (cTag differs from saved cTag) **AND**
- Local file changed (modification time > last sync time)

### Detection Algorithm

```csharp
bool BothSidesChanged(FileMetadata? localMetadata, RemoteChange remoteChange)
{
    if (localMetadata == null)
        return false; // New remote file, no conflict
    
    // Check if remote changed
    bool remoteChanged = remoteChange.CTag != localMetadata.CTag;
    
    // Check if local changed since last sync
    var localFile = new FileInfo(localMetadata.LocalPath);
    bool localChanged = localFile.LastWriteTimeUtc > localMetadata.LastModifiedUtc;
    
    return remoteChanged && localChanged;
}
```

### Why cTag + Timestamp?

| Approach | Pros | Cons |
|----------|------|------|
| **SHA256 Hash** | 100% accurate | Slow (must read entire file), CPU intensive |
| **Timestamp + Size** | Very fast | Can miss changes if timestamp forged |
| **OneDrive cTag** | Fast, accurate, provided by OneDrive | Requires API metadata |

**Chosen Strategy**: **cTag + Timestamp**

- Remote: Use OneDrive's `cTag` (changes with content)
- Local: Compare file modification timestamp
- Only compute SHA256 for local files being uploaded (not for conflict detection)

---

## Conflict Resolution

### Resolution Strategies

```csharp
public enum ConflictResolutionStrategy
{
    Unresolved,    // User hasn't chosen yet
    KeepLocal,     // Upload local, overwrite remote
    KeepRemote,    // Download remote, overwrite local
    KeepBoth,      // Rename local, download remote
    Skip           // Don't sync this file
}
```

### Strategy Implementation

#### KeepLocal

```csharp
// Upload local version to OneDrive
await UploadFile(localPath, remotePath);
// Remote file now matches local
```

#### KeepRemote

```csharp
// Download remote version to local
await DownloadFile(remoteFileId, localPath);
// Local file now matches remote
```

#### KeepBoth

```csharp
// Rename local file with timestamp
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var newName = $"{fileNameWithoutExt}_conflict_{timestamp}{extension}";
File.Move(localPath, newName);

// Download remote to original path
await DownloadFile(remoteFileId, localPath);
// Result: Both versions preserved
```

#### Skip

```csharp
// Do nothing - leave both versions as-is
// File remains in conflict state for future resolution
```

---

## State Management

### Sync States

```csharp
public enum SyncStatus
{
    Idle,          // Not syncing
    Running,       // Actively syncing
    Paused,        // User paused, can resume
    Completed,     // Finished successfully
    Failed         // Error occurred
}
```

### File Sync States

```csharp
public enum FileSyncStatus
{
    NotSynced,        // Never synced
    Synced,           // In sync on both sides
    PendingUpload,    // Local change, needs upload
    PendingDownload,  // Remote change, needs download
    Conflict          // Both sides changed
}
```

### State Transitions

``` text
┌──────────┐  Start Sync   ┌─────────┐
│   Idle   ├──────────────►│ Running │
└──────────┘               └────┬────┘
                                │
                    ┌───────────┼───────────┐
                    │           │           │
              ┌─────▼──────┐    │    ┌──────▼─────┐
              │   Paused   │    │    │  Completed │
              └─────┬──────┘    │    └────────────┘
                    │           │
                    │      ┌────▼────┐
                    └──────┤  Failed │
                           └─────────┘
```

### Persistence

**Database Tables**:

1. **Accounts**: Stores delta tokens per account
2. **FileMetadata**: Tracks every synced file's cTag, timestamp, size
3. **SyncStates**: Current progress (files completed, bytes transferred, ETA)
4. **Conflicts**: Unresolved conflicts awaiting user input

**Resume Capability**:

```csharp
// On app restart or resume
var savedState = await LoadSyncState(accountId);
if (savedState.Status == SyncStatus.Paused)
{
    // Resume from where we left off
    await DeltaSyncEngine.StartSyncAsync(accountId);
    // Delta token ensures we don't re-download already synced files
}
```

---

## Progress Tracking

### Metrics Calculated

```csharp
public record SyncProgress
{
    // File counts
    int TotalFiles;
    int CompletedFiles;
    int FilesDownloading;
    int FilesUploading;
    int ConflictsDetected;
    
    // Byte counts
    long TotalBytes;
    long CompletedBytes;
    
    // Performance
    double MegabytesPerSecond;
    TimeSpan? EstimatedTimeRemaining;
}
```

### ETA Calculation

```csharp
double CalculateMBps(long completedBytes, TimeSpan elapsed)
{
    if (elapsed.TotalSeconds == 0)
        return 0;
    
    return (completedBytes / 1_048_576.0) / elapsed.TotalSeconds;
}

TimeSpan? CalculateETA(long totalBytes, long completedBytes, double mbps)
{
    if (mbps == 0 || totalBytes <= completedBytes)
        return null;
    
    var remainingBytes = totalBytes - completedBytes;
    var remainingMB = remainingBytes / 1_048_576.0;
    var secondsRemaining = remainingMB / mbps;
    
    return TimeSpan.FromSeconds(secondsRemaining);
}
```

### Update Frequency

- **Every 10 files**: Update database and notify UI
- **Every 1 second**: If less than 10 files processed
- **Rationale**: Balance between responsiveness and database write overhead

---

## Error Handling

### Retry Strategy

```csharp
// Network errors: Exponential backoff
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (exception, timeSpan, attempt, context) =>
        {
            _logger.LogWarning(
                "Retry {Attempt} after {Delay}s due to {Exception}",
                attempt, timeSpan.TotalSeconds, exception.Message);
        });

await retryPolicy.ExecuteAsync(() => DownloadFileAsync(fileId));
```

### Common Scenarios

| Error | Handling |
|-------|----------|
| **Network timeout** | Retry with exponential backoff (3 attempts) |
| **Token expired** | Refresh token silently, retry operation |
| **File locked** | Skip file, mark as failed, continue with others |
| **Insufficient space** | Pause sync, notify user, prompt to free space |
| **Permission denied** | Log error, skip file, notify user |
| **OneDrive quota exceeded** | Pause uploads, notify user |

---

## Performance Optimizations

### 1. Parallel Downloads/Uploads

```csharp
// Process up to 4 files concurrently
var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

await Parallel.ForEachAsync(filesToDownload, parallelOptions, async (file, ct) =>
{
    await DownloadFileAsync(file, ct);
});
```

### 2. Large File Chunking

```csharp
// For files > 4MB, use resumable upload session
if (fileSize > 4 * 1024 * 1024)
{
    var uploadSession = await CreateUploadSessionAsync(fileName);
    await UploadInChunksAsync(uploadSession, fileStream, chunkSize: 320 * 1024);
}
else
{
    await UploadDirectAsync(fileStream);
}
```

### 3. Database Batching

```csharp
// Batch metadata updates every 10 files
var metadataBatch = new List<FileMetadata>();

foreach (var file in files)
{
    metadataBatch.Add(ProcessFile(file));
    
    if (metadataBatch.Count >= 10)
    {
        await SaveMetadataBatchAsync(metadataBatch);
        metadataBatch.Clear();
    }
}
```

### 4. Delta Token Caching

```csharp
// Cache delta token in memory + database
// Avoids hitting database on every sync check
private readonly Dictionary<string, string> _deltaTokenCache = new();

string GetDeltaToken(string accountId)
{
    if (_deltaTokenCache.TryGetValue(accountId, out var token))
        return token;
    
    token = await LoadDeltaTokenFromDatabase(accountId);
    _deltaTokenCache[accountId] = token;
    return token;
}
```

---

## Testing Scenarios

### Unit Tests

| Scenario | Expected Outcome |
|----------|------------------|
| Remote file added | File downloaded to local |
| Local file added | File uploaded to OneDrive |
| Remote file modified | Local file updated |
| Local file modified | OneDrive file updated |
| Remote file deleted | Local file deleted |
| Local file deleted | OneDrive file deleted |
| Both modified (conflict) | Conflict recorded, sync paused |
| Conflict resolved (keep local) | Local uploaded, conflict cleared |
| Conflict resolved (keep remote) | Remote downloaded, conflict cleared |
| Conflict resolved (keep both) | Local renamed, remote downloaded |
| Network failure during download | Retry 3 times, then fail gracefully |
| Token expired | Refresh token, resume sync |

### Integration Tests

1. **Full Sync Workflow**: Add account → Select folders → Initial sync → Verify all files
2. **Bidirectional Changes**: Modify files locally and remotely → Sync → Verify both updated
3. **Conflict Resolution**: Change same file on both sides → Detect conflict → Resolve → Verify
4. **Pause/Resume**: Start sync → Pause mid-way → Restart app → Resume → Verify completion
5. **Delta Sync**: Sync once → Change single file → Sync again → Verify only 1 file processed

---

## Key Algorithms Summary

### 1. Delta Query Loop

```
WHILE syncInProgress:
    remoteChanges = FetchDelta(deltaToken)
    FOR EACH change IN remoteChanges:
        IF conflict:
            RecordConflict(change)
        ELSE:
            ApplyChange(change)
    deltaToken = remoteChanges.newToken
    SaveDeltaToken(deltaToken)
```

### 2. Conflict Detection

``` text
FUNCTION IsConflict(localMeta, remoteChange):
    remoteDifferent = remoteChange.cTag != localMeta.cTag
    localFile = GetFileInfo(localMeta.path)
    localDifferent = localFile.modifiedTime > localMeta.lastSyncTime
    RETURN remoteDifferent AND localDifferent
```

### 3. Tri-State Checkbox Sync

``` text
FUNCTION GetSelectedFolders(rootNodes):
    selected = []
    FOR EACH node IN rootNodes:
        IF node.isSelected == TRUE:
            selected.Add(node.path)
        ELSE IF node.isSelected == NULL:  // Indeterminate
            selected.AddRange(GetSelectedFolders(node.children))
    RETURN selected
```

---

## Sequence Diagrams

### Initial Sync (First Time)

``` text
User               SyncEngine         Graph API         Database
 |                     |                  |                 |
 |-- Start Sync ------>|                  |                 |
 |                     |-- GET /delta --->|                 |
 |                     |<-- All Items ----|                 |
 |                     |                  |                 |
 |                     |-- Download ------|--> Save File -->|
 |                     |<- File Content --|                 |
 |                     |                  |                 |
 |                     |-- Save Metadata----------------->  |
 |                     |-- Save DeltaToken-------------->  |
 |<-- Sync Complete ---|                  |                 |
```

### Incremental Sync (Subsequent)

``` text
User               SyncEngine         Graph API         Database
 |                     |                  |                 |
 |-- Start Sync ------>|                  |                 |
 |                     |-- Load DeltaToken--------------->  |
 |                     |<- Token -----------|               |
 |                     |-- GET /delta?token->|              |
 |                     |<-- Changes Only ----|              |
 |                     |                  |                 |
 |                     |-- Process Changes|                 |
 |                     |-- Update Metadata--------------->  |
 |<-- Sync Complete ---|                  |                 |
```

### Conflict Detection & Resolution

``` text
User              SyncEngine        Graph API       Database       UI
 |                    |                 |               |           |
 |-- Start Sync ----->|                 |               |           |
 |                    |-- Remote Δ ---->|               |           |
 |                    |<-- Changed -----|               |           |
 |                    |-- Check Local----------------->  |           |
 |                    |<-- Modified ----|               |           |
 |                    |                 |               |           |
 |                    |-- Conflict! -------------------->|           |
 |                    |-- Notify UI ---------------------------->   |
 |                    |                 |               |           |
 |<-- Conflict Detected (pause) --------|               |           |
 |                    |                 |               |           |
 |-- Resolve (KeepLocal) -------------->|               |           |
 |                    |-- Upload ------>|               |           |
 |                    |-- Clear Conflict-------------->  |           |
 |<-- Resolved -------|                 |               |           |
```

---

## References

### Microsoft Graph API Documentation

- [Delta Query Overview](https://learn.microsoft.com/en-us/graph/delta-query-overview)
- [OneDrive Sync](https://learn.microsoft.com/en-us/graph/api/driveitem-delta)
- [Upload Large Files](https://learn.microsoft.com/en-us/graph/api/driveitem-createuploadsession)

### Related Design Documents

- [Implementation Plan](./multi-account-ux-implementation-plan.md)
- Project Coding Standards: `.github/copilot-instructions.md`

---

**Document Version**: 1.0
**Last Updated**: January 5, 2026
**Status**: Ready for Implementation
