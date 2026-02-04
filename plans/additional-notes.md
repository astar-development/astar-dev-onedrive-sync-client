# UI/UX Flow

## First Launch

``` text
App Start
  ↓
Main Window (Empty)
  ↓
"Add Account" Button (center of window)
  ↓
Click → OAuth Browser Flow
  ↓
User Authenticates (Microsoft)
  ↓
App captures token, stores securely
  ↓
Add account to database with default settings (HomeSyncDirectory, MaxConcurrent=5)
  ↓
Refresh UI → Account List (left sidebar)
```

## Subsequent Launches

``` text
App Start
  ↓
Load Accounts from database
  ↓
Display Account List (left sidebar)
  ↓
Select Account
  ↓
Display Folder Tree (main panel)
  ↓
Empty initially (first sync shows root)
  ↓
Folder tree populated after first sync
  ↓
User checks folders to sync
  ↓
"Start Sync" button → bidirectional sync triggered
  ↓
Background scheduler continues periodic bidirectional checks
  ↓
If conflicts found → show conflict resolution dialog
```

## UI Layout (Updated)

``` text
┌─────────────────────────────────────────────┐
│ OneDrive Sync Client     [View Logs] [⚙]   │
├──────────────┬──────────────────────────────┤
│              │                              │
│ Accounts     │    Folder Selection          │
│ ─────────    │    ──────────────            │
│ [+] Add Acc. │    ☐ Documents               │
│              │    ☐ Pictures                │
│ ☑ user1@...  │    ☐ Desktop                 │
│   [Edit]     │                              │
│ ☐ user2@...  │    [Start Sync] [Settings]   │
│   [Edit]     │                              │
│              │    Sync Status: Syncing      │
│              │    ↓ 12 files | ↑ 3 files    │
│              │    Last Sync: 2 min ago      │
│              │                              │
└──────────────┴──────────────────────────────┘
```

### Home Screen Menu Items

- **View Logs**: Opens log viewer dialog with account selector
- **Settings (⚙)**: Global app settings (sync interval, etc.)

### Per-Account Actions

- **[Edit]**: Opens Edit Account dialog with:
  - Home Sync Directory (folder picker)
  - Concurrent Downloads (slider: 1-20, default 5)
  - Concurrent Uploads (slider: 1-20, default 5)
  - Enable Debug Logging (toggle)
  - Disconnect Account (button)

## Configuration & Settings

**App Settings (`appsettings.json`)**

```json
{
  "OneDrive": {
    "ClientId": "xxxxx",
    "Authority": "https://login.microsoftonline.com/common",
    "Scopes": ["https://graph.microsoft.com/.default"]
  },
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=onedrive_sync;Username=postgres;Password=xxxxx",
    "Schema": "onedrive"
  },
  "Sync": {
    "DefaultHomeSyncDirectory": "{UserFolder}/OneDriveSyncData",
    "DefaultMaxConcurrentDownloads": 5,
    "DefaultMaxConcurrentUploads": 5,
    "DefaultScheduleIntervalSeconds": 300
  },
  "Telemetry": {
    "Enabled": true,
    "ExportToDatabase": true,
    "ExportToFile": true,
    "LogFilePath": "{AppDataFolder}/logs/traces.json"
  },
  "Logging": {
    "LogLevel": "Information",
    "DefaultLogLevel": "Warning"
  },
  "LogViewer": {
    "DefaultPageSize": 100,
    "MaxPageSize": 500
  }
}
```

### Environment Variables

- `ONEDRIVE_SYNC_CLIENT_ID`: OAuth client ID
- `ONEDRIVE_SYNC_DB_CONNECTION`: Override database connection string
- `ONEDRIVE_SYNC_LOCAL_STORAGE`: Override default home sync directory
- `ONEDRIVE_SYNC_SCHEDULE_INTERVAL`: Override sync interval (seconds)
- `ONEDRIVE_SYNC_MAX_CONCURRENT`: Override default concurrent operations

## Security Considerations

1. **Token Security**: Tokens stored in platform-native secure storage (DPAPI/Keychain/SecretService), never in plain text
2. **API Calls**: All Graph API calls use HTTPS; token refresh proactive to avoid expired-token errors
3. **GDPR Compliance**:
   - Both email and Microsoft account ID hashed using SHA256 before database storage
   - No plaintext PII persisted in database
   - Secure storage used for any display-only email addresses (encrypted at rest)
   - Right to erasure: account deletion removes all database records and secure storage entries
4. **Data Privacy**: Account information hashed; plaintext email/account ID not stored in database
5. **Local Files**: Synced files stored in user-configurable home sync directory with file-level permissions
6. **Conflict Resolution**: User explicitly chooses resolution; no automatic overwrites without warning
7. **Database Security**: PostgreSQL connection string stored securely; use environment variables for production
8. **Concurrent Operations**: Limits prevent resource exhaustion and API throttling

## Testing Strategy

**Unit Tests** (target 95% coverage for core domain)

### Framework & Tooling

- **Testing Framework**: xUnit
- **Mocking**: NSubstitute for creating test doubles and verifying interactions
- **Assertions**: Shouldly for fluent, readable assertions

### Test Coverage by Component

1. **AuthenticationService Tests**
   - Token refresh triggers proactively before expiry (< 5 min remaining)
   - Expired tokens refreshed on-demand with exponential backoff
   - Failed refresh attempts logged and retried appropriately
   - Token storage integration verified (mock `ISecureTokenStorage`)
   - OAuth flow captures correct claims (email, account ID, expiry)
   - Concurrent refresh requests deduplicated (single refresh for multiple callers)
   - Example assertion: `tokenRefreshTime.ShouldBeLessThan(expiryTime.AddMinutes(-5))`

2. **DeltaSyncService Tests**
   - Delta token correctly saved after successful sync
   - Empty delta token triggers full sync (initial state)
   - Remote changes parsed and mapped to FileSystemItems
   - Deleted items handled (remove from local tracking)
   - Graph API errors handled gracefully (transient vs. permanent)
   - Mock Graph API responses for various scenarios
   - Example assertion: `deltaToken.ShouldNotBeNullOrEmpty()`

3. **LocalChangeDetectionService Tests**
   - FileSystemWatcher events trigger hash comparison
   - Hash mismatch detected between local and cached state
   - New files detected and queued for upload
   - Deleted files detected and flagged for remote deletion
   - Renamed files detected via parent directory + name change
   - Large file changes debounced (avoid multiple events per edit)
   - Example assertion: `changedFiles.Count.ShouldBe(expectedCount)`

4. **ConflictDetectionService Tests**
   - Local newer: LocalModifiedAt > RemoteModifiedAt → conflict type `local_newer`
   - Remote newer: RemoteModifiedAt > LocalModifiedAt → conflict type `remote_newer`
   - Both modified: different hashes + recent timestamps → conflict type `both_modified`
   - No conflict: hashes match → skip conflict log
   - Timestamp tolerance (within 2 seconds) to avoid false positives
   - Example assertion: `conflictType.ShouldBe(ConflictType.BothModified)`

5. **LocalFileOperationService Tests**
   - Copy file to local directory with correct permissions
   - Delete file handles non-existent files gracefully
   - Rename file updates path in FileSystemItems
   - IO exceptions handled (file locked, permissions denied)
   - Large file copy progress reported correctly
   - Directory creation for nested paths
   - Example assertion: `File.Exists(localPath).ShouldBeTrue()`

6. **RemoteFileOperationService Tests**
   - Upload file via Graph API with multipart upload for large files
   - Delete file via Graph API handles 404 gracefully
   - Rename file via Graph API updates remote metadata
   - API throttling handled with retry + exponential backoff
   - Upload progress reported via callbacks
   - Example assertion: `uploadResponse.StatusCode.ShouldBe(HttpStatusCode.Created)`

7. **ConcurrentDownloadQueue Tests**
   - Semaphore limits concurrent downloads to configured max (default 5)
   - Queue processes items in FIFO order
   - Failed downloads retried with exponential backoff
   - Progress tracking for each queued item
   - Queue drains completely before shutdown
   - Example assertion: `activeDownloads.ShouldBeLessThanOrEqualTo(maxConcurrent)`

8. **ConcurrentUploadQueue Tests**
   - Semaphore limits concurrent uploads to configured max (default 5)
   - Queue processes items in FIFO order
   - Failed uploads retried with exponential backoff
   - Progress tracking for each queued item
   - Queue drains completely before shutdown
   - Example assertion: `activeUploads.ShouldBeLessThanOrEqualTo(maxConcurrent)`

9. **AccountRepository Tests**
   - Create account with hashed email and account ID
   - Retrieve account by hashed ID
   - Update account settings (HomeSyncDirectory, MaxConcurrent, DebugLogging)
   - Delete account removes all foreign key references
   - Unique constraint enforced on HashedEmail
   - Example assertion: `account.HashedEmail.ShouldNotBeNullOrEmpty()`

10. **FileSystemRepository Tests**
    - Insert FileSystemItem with all required fields
    - Update IsSelected flag for folder selection
    - Query selected folders for account
    - Update sync status (synced, pending, conflict)
    - Cascade delete when account removed
    - Example assertion: `selectedFolders.ShouldAllBe(f => f.IsSelected)`

11. **DeltaTokenRepository Tests**
    - Save delta token per account and drive
    - Retrieve latest delta token for sync
    - Update delta token after successful sync
    - Handle multiple drives per account
    - Example assertion: `deltaToken.LastSyncAt.ShouldBeGreaterThan(previousSyncTime)`

12. **ConflictRepository Tests**
    - Log conflict with all metadata
    - Retrieve unresolved conflicts for UI display
    - Update conflict with resolution action
    - Mark conflict as resolved with timestamp
    - Example assertion: `unresolvedConflicts.ShouldNotBeEmpty()`

13. **LogViewerService Tests**
    - Paged query returns correct page size
    - Filter by account ID works correctly
    - Filter by log level works correctly
    - Search by message text works correctly
    - Total count accurate for paging UI
    - Example assertion: `pagedResult.Items.Count.ShouldBe(pageSize)`

14. **Hashing Service Tests** (new component)
    - Email hashed consistently (case-insensitive)
    - Account ID hashed with salt (createdAtTicks)
    - SHA256 hash format validated
    - Hash collision handling (should be extremely rare)
    - Example assertion: `hashedEmail.Length.ShouldBe(64)` (SHA256 hex length)

**Integration Tests** (target 85% coverage for repositories)

- `AccountRepository`: create/read/update/delete accounts, custom settings
- `FileSystemRepository`: manage folder tree, selection state, hash tracking
- `DeltaTokenRepository`: save/load delta tokens
- `ConflictRepository`: log and retrieve conflicts
- `LogViewerRepository`: paged queries, filtering

**UI Tests** (ReactiveUI ViewModel testing)

- `AddAccountViewModel`: reactive state transitions
- `EditAccountViewModel`: settings validation, reactive updates
- `FolderSelectionViewModel`: folder selection state, IsSelected propagation
- `ConflictResolutionViewModel`: dialog state, user action handling
- `LogViewerViewModel`: paging logic, filtering, account selection

**End-to-End Tests** (BDD scenarios with SpecFlow or similar)

## Framework & Tooling 2

- **BDD Framework**: SpecFlow (integrates with xUnit)
- **Assertions**: Shouldly for readable BDD assertions
- **Test Data**: Use in-memory PostgreSQL (TestContainers) or SQLite for test isolation
- **Mocking**: NSubstitute for external dependencies (Graph API)

## BDD Feature Files

### Feature 1: Account Management

```gherkin
Feature: Account Management
  As a user
  I want to add and manage OneDrive accounts
  So that I can sync multiple accounts

Scenario: Add new account successfully
  Given the application is launched for the first time
  When I click "Add Account"
  And I authenticate with Microsoft credentials
  Then the account should appear in the account list
  And the account should have default settings (MaxConcurrent=5)
  And the email and account ID should be hashed in the database

Scenario: Edit account settings
  Given an account "user@example.com" exists
  When I click "Edit" for the account
  And I set HomeSyncDirectory to "/custom/path"
  And I set MaxConcurrentDownloads to 10
  And I enable debug logging
  And I save the settings
  Then the account settings should be persisted
  And the debug logging should be enabled for that account

Scenario: Remove account
  Given an account "user@example.com" exists
  When I disconnect the account
  Then the account should be removed from the list
  And all related data should be deleted (GDPR compliance)
  And secure storage should be cleared for that account
```

### Feature 2: Folder Selection and Sync

```gherkin
Feature: Folder Selection and Sync
  As a user
  I want to select specific folders to sync
  So that I can control which files are synced locally

Scenario: Select folders for sync
  Given an account "user@example.com" is connected
  And the folder tree is displayed
  When I check "Documents" folder
  And I check "Pictures" folder
  And I click "Start Sync"
  Then the selected folders should be marked as IsSelected=true
  And the sync should start for those folders only

Scenario: Bidirectional sync downloads remote changes
  Given folders "Documents" and "Pictures" are selected
  And remote OneDrive has 5 new files in "Documents"
  When the sync runs
  Then 5 files should be downloaded to local directory
  And FileSystemItems should be updated with local paths
  And SyncHistory should record 5 downloads

Scenario: Bidirectional sync uploads local changes
  Given folders "Documents" is selected and synced
  And I create 3 new files locally in "Documents"
  When the sync runs
  Then 3 files should be uploaded to OneDrive
  And FileSystemItems should be updated with remote IDs
  And SyncHistory should record 3 uploads
```

### Feature 3: Conflict Resolution

```gherkin
Feature: Conflict Resolution
  As a user
  I want to resolve sync conflicts
  So that I can decide which version to keep

Scenario: Detect conflict when both local and remote modified
  Given a file "document.txt" is synced
  And the file is modified locally
  And the file is modified remotely
  When the sync runs
  Then a conflict should be detected
  And the conflict should be logged in ConflictLogs
  And the user should be prompted to resolve the conflict

Scenario: Resolve conflict by keeping local version
  Given a conflict exists for "document.txt"
  When the conflict dialog is shown
  And I select "Keep Local"
  Then the local version should be uploaded to OneDrive
  And the conflict should be marked as resolved
  And ResolutionAction should be "keep_local"

Scenario: Resolve conflict by keeping both versions
  Given a conflict exists for "document.txt"
  When I select "Keep Both"
  Then the local version should be renamed to "document_local.txt"
  And the remote version should be downloaded as "document.txt"
  And the conflict should be marked as resolved
```

### Feature 4: Background Scheduling

```gherkin
Feature: Background Scheduling
  As a user
  I want automatic periodic syncs
  So that my files stay up to date without manual intervention

Scenario: Background sync runs on schedule
  Given sync interval is configured to 5 minutes
  And an account with selected folders exists
  When 5 minutes elapse
  Then a background sync should be triggered
  And SyncType should be "scheduled"
  And any changes should be synced bidirectionally

Scenario: Background sync detects and queues conflicts
  Given a background sync is running
  And a conflict is detected
  When the sync completes
  Then the conflict should be logged
  And the user should be notified
  And the conflict dialog should be shown
```

### Feature 5: Log Viewer

```gherkin
Feature: Log Viewer
  As a user
  I want to view application logs
  So that I can troubleshoot issues and monitor activity

Scenario: View logs for specific account
  Given multiple accounts exist
  And logs exist for "user@example.com"
  When I click "View Logs"
  And I select "user@example.com" from the dropdown
  Then logs for that account should be displayed
  And logs should be paged (100 rows per page)

Scenario: Filter logs by level
  Given logs exist with various levels
  When I filter by "Error" level
  Then only error logs should be displayed
  And the count should reflect filtered results

Scenario: Navigate log pages
  Given 500 log entries exist
  And page size is 100
  When I navigate to page 2
  Then rows 101-200 should be displayed
  And page indicator should show "Page 2 of 5"
```

### Feature 6: Concurrent Operations

```gherkin
Feature: Concurrent Operations
  As a user
  I want to configure concurrent upload/download limits
  So that I can optimize sync performance for my network

Scenario: Respect concurrent download limit
  Given MaxConcurrentDownloads is set to 5
  And 20 files are queued for download
  When the download queue processes
  Then at most 5 downloads should be active simultaneously
  And remaining files should wait in queue

Scenario: Respect concurrent upload limit
  Given MaxConcurrentUploads is set to 3
  And 10 files are queued for upload
  When the upload queue processes
  Then at most 3 uploads should be active simultaneously
  And remaining files should wait in queue
```

### Feature 7: GDPR Compliance

```gherkin
Feature: GDPR Compliance
  As a user
  I want my personal data to be handled securely
  So that I comply with GDPR regulations

Scenario: Email and account ID are hashed
  Given I add an account with email "user@example.com"
  When the account is saved to the database
  Then the email should be hashed using SHA256
  And the Microsoft account ID should be hashed using SHA256
  And no plaintext PII should exist in the database

Scenario: Right to erasure
  Given an account exists with synced data
  When I disconnect the account
  Then all database records should be deleted
  And secure storage entries should be cleared
  And no PII should remain in the system
```

### Performance Tests

- Concurrent downloads/uploads with various limits (1, 5, 10, 20)
- Large file sync (1GB+ files)
- Many small files (10k+ files)
- PostgreSQL query performance (indexing, paging)
- Log viewer paging with large datasets (1M+ log entries)

## Dependencies & NuGet Packages

- **Microsoft.Identity.Client**: OAuth authentication
- **Microsoft.Kiota.Abstractions**: Kiota-generated Graph API client
- **Microsoft.Graph**: Microsoft Graph API models
- **Microsoft.EntityFrameworkCore**: ORM
- **Npgsql.EntityFrameworkCore.PostgreSQL**: PostgreSQL provider
- **Avalonia**: UI framework
- **Avalonia.ReactiveUI**: MVVM integration
- **ReactiveUI**: Reactive MVVM framework
- **OpenTelemetry**: Observability
- **OpenTelemetry.Exporter.InMemory**: For testing
- **Tmds.DBus** (or equivalent): Linux SecretService (conditional)
- **Serilog**: Structured logging (optional, recommended)
- **Serilog.Sinks.PostgreSQL**: PostgreSQL logging sink
- **xUnit**: Testing framework (already in place)
- **NSubstitute**: Mocking library for unit tests
- **Shouldly**: Assertion library for unit and BDD tests
- **SpecFlow**: BDD framework for Gherkin feature files (integrates with xUnit)
- **SpecFlow.xUnit**: xUnit integration for SpecFlow
- **Testcontainers**: For integration tests with real PostgreSQL instances
- **System.IO.Hashing**: For file hash computation (XXHash or similar)

## Notes & Future Enhancements

1. **Bandwidth Throttling**: Limit sync to avoid overwhelming network
2. **Selective Sync Resume**: If sync interrupted, resume from last known state
3. **Version Control**: Store file version IDs to detect renames vs. deletes
4. **User Notifications**: Desktop notifications for sync completion, conflicts
5. **Analytics Dashboard**: View sync history, conflict statistics, performance metrics
6. **Multi-Tenancy**: Currently supports Personal Accounts; consider Work/School accounts later
7. **Advanced Conflict Resolution**: Three-way merge for text files
8. **Log Export**: Export logs to CSV/JSON for external analysis
9. **Performance Monitoring**: Real-time metrics dashboard in UI
10. **Cloud Database**: Option to use cloud-hosted PostgreSQL (Azure, AWS RDS) for centralized logging across devices
