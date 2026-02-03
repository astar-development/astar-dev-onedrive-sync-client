# OneDrive Sync Client - Architecture & Implementation Plan

## Overview

Build a cross-platform OneDrive sync client using .NET 10, AvaloniaUI, and ReactiveUI with a layered, feature-sliced architecture. Implement secure multi-account support with proactive OAuth token refresh, **bidirectional (two-way) folder syncing** via delta tokens, and background sync scheduling. Store data in a **PostgreSQL database** with `onedrive` schema, support configurable local storage per account, **configurable concurrent up/download operations**, and provide per-user diagnostic logging and telemetry (database-first, fallback to local file).

## Architecture Overview

### Layered Architecture

```
┌──────────────────────────────────────────┐
│  Presentation Layer (AvaloniaUI)         │
│  - Views                                 │
│  - ViewModels (ReactiveUI)               │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│  Application Layer                       │
│  - Use Cases / Orchestration             │
│  - Command Handlers                      │
│  - Application Services                  │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│  Domain Layer                            │
│  - Domain Models                         │
│  - Domain Interfaces                     │
│  - Domain Services                       │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│  Infrastructure Layer                    │
│  - EF Core & PostgreSQL                  │
│  - Secure Token Storage                  │
│  - Microsoft Graph API Client (Kiota V5) │
│  - File System Operations                │
│  - Telemetry & Logging                   │
└──────────────────────────────────────────┘
```

### Vertical-Slice Feature Structure

Organize the codebase by independent features rather than technical layers. Each slice contains all layers for a single feature:

```
src/
├── AStar.Dev.OneDrive.Sync.Client/
│   ├── Features/
│   │   ├── Authentication/
│   │   │   ├── Controllers/ (UI entry points)
│   │   │   ├── ViewModels/
│   │   │   ├── Services/
│   │   │   ├── Models/
│   │   │   ├── Repositories/
│   │   │   └── OAuth/ (token handling, refresh logic)
│   │   │
│   │   ├── AccountManagement/
│   │   │   ├── Controllers/
│   │   │   ├── ViewModels/
│   │   │   ├── Services/
│   │   │   ├── Models/
│   │   │   └── Repositories/
│   │   │
│   │   ├── FileSync/
│   │   │   ├── Controllers/
│   │   │   ├── ViewModels/
│   │   │   ├── Services/
│   │   │   ├── Models/
│   │   │   ├── Repositories/
│   │   │   ├── DeltaSync/ (delta token logic)
│   │   │   ├── LocalFileOperations/
│   │   │   ├── UploadQueue/ (concurrent upload orchestration)
│   │   │   └── DownloadQueue/ (concurrent download orchestration)
│   │   │
│   │   ├── ConflictResolution/
│   │   │   ├── Controllers/ (UI dialogs)
│   │   │   ├── ViewModels/
│   │   │   ├── Services/
│   │   │   ├── Models/
│   │   │   └── Repositories/
│   │   │
│   │   ├── Scheduling/
│   │   │   ├── Services/
│   │   │   ├── Models/
│   │   │   └── BackgroundWorkers/
│   │   │
│   │   ├── Telemetry/
│   │   │   ├── Services/
│   │   │   ├── Logging/
│   │   │   ├── Observability/ (OpenTelemetry configuration)
│   │   │   └── Diagnostics/
│   │   │
│   │   └── LogViewer/
│   │       ├── Controllers/
│   │       ├── ViewModels/
│   │       ├── Services/
│   │       └── Models/
│   │
│   ├── Infrastructure/
│   │   ├── Database/
│   │   │   ├── DbContext/
│   │   │   ├── Migrations/
│   │   │   └── Seeding/
│   │   ├── SecureStorage/
│   │   ├── GraphApi/
│   │   └── Configuration/
│   │
│   ├── Common/
│   │   ├── Models/
│   │   ├── Extensions/
│   │   ├── Constants/
│   │   ├── Exceptions/
│   │   └── Utilities/
│   │
│   ├── Views/ (top-level Avalonia UI)
│   ├── App.xaml
│   ├── Program.cs
│   └── AppModule.cs (DI registration)
```

## Core Design Decisions

### 1. Authentication & Secure Token Storage

**OAuth 2.0 Flow for Microsoft Personal Accounts**
- Use MSAL (Microsoft Authentication Library) for token acquisition
- Implement Device Code Flow or Interactive Browser Flow for user authentication
- Proactive token refresh: check expiry before each Graph API call; refresh if expiry < 5 minutes

**Cross-Platform Secure Storage**
- Abstract interface: `ISecureTokenStorage`
- Platform-specific implementations:
  - **Windows**: DPAPI (System.Security.Cryptography.DataProtectionScope.CurrentUser)
  - **macOS**: Keychain (via Foundation/Security frameworks or native wrapper)
  - **Linux**: SecretService dbus protocol (via a third-party library like `Tmds.DBus`)
- Fallback: Encrypted file storage using `System.Security.Cryptography.Aes` if platform-specific unavailable
- Factory pattern to select implementation at runtime

**Token Refresh Strategy**
- Background task checks token expiry every 5 minutes
- Proactive refresh 5 minutes before expiry
- On-demand refresh if expired (with exponential backoff on transient failures)

### 2. Database Design

**Schema Overview** (PostgreSQL with EF Core, using `onedrive` schema)

**PostgreSQL Setup**
- All tables reside in the `onedrive` schema for isolation
- Connection string configuration in `appsettings.json`
- EF Core migrations applied with schema specification
- Schema creation handled automatically via migrations

```sql
-- Core account table
CREATE TABLE onedrive.Accounts (
    Id TEXT PRIMARY KEY,                    -- Hashed AccountId (SHA256 of email + timestamp)
    HashedEmail TEXT NOT NULL UNIQUE,       -- Hashed email for lookups
    DisplayName TEXT,
    TokenStorageKey TEXT,                   -- Reference to secure storage
    HomeSyncDirectory TEXT,                 -- User-configurable local sync path
    MaxConcurrentDownloads INT DEFAULT 5,   -- Concurrent download limit
    MaxConcurrentUploads INT DEFAULT 5,     -- Concurrent upload limit
    EnableDebugLogging BOOLEAN DEFAULT FALSE, -- Per-account debug logging
    CreatedAt TIMESTAMP,
    LastAuthRefresh TIMESTAMP,
    IsActive BOOLEAN
);

-- Delta token per account (for incremental sync)
CREATE TABLE onedrive.DeltaTokens (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    DriveName TEXT NOT NULL,                -- e.g., "root", "documents", "photos"
    Token TEXT,                             -- Opaque delta token from Graph API
    LastSyncAt TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- File/folder tree with selection flag
CREATE TABLE onedrive.FileSystemItems (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    DriveItemId TEXT NOT NULL,              -- OneDrive item ID
    Name TEXT,
    Path TEXT,
    IsFolder BOOLEAN,
    ParentItemId TEXT,                      -- NULL for root
    IsSelected BOOLEAN DEFAULT FALSE,       -- User selection flag
    LocalPath TEXT,                         -- Synced to this local path
    RemoteModifiedAt TIMESTAMP,
    LocalModifiedAt TIMESTAMP,
    RemoteHash TEXT,                        -- Remote file hash (for change detection)
    LocalHash TEXT,                         -- Local file hash (for change detection)
    SyncStatus TEXT,                        -- 'synced', 'pending_upload', 'pending_download', 'conflict', 'failed'
    LastSyncDirection TEXT,                 -- 'upload', 'download', 'bidirectional'
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Conflict log for resolution
CREATE TABLE onedrive.ConflictLogs (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    ItemId TEXT NOT NULL,
    LocalPath TEXT,
    ConflictType TEXT,                      -- 'local_newer', 'remote_newer', 'both_modified'
    LocalLastModified TIMESTAMP,
    RemoteLastModified TIMESTAMP,
    ResolutionAction TEXT,                  -- 'keep_local', 'keep_remote', 'both', 'ignore'
    ResolvedAt TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Sync history for auditing
CREATE TABLE onedrive.SyncHistory (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    SyncType TEXT,                          -- 'manual', 'scheduled', 'background'
    SyncDirection TEXT,                     -- 'upload', 'download', 'bidirectional'
    StartedAt TIMESTAMP,
    CompletedAt TIMESTAMP,
    Status TEXT,                            -- 'success', 'partial', 'failed'
    ItemsUploaded INT,
    ItemsDownloaded INT,
    ErrorMessage TEXT,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- User diagnostic settings
CREATE TABLE onedrive.DiagnosticSettings (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL UNIQUE,
    LogLevel TEXT,                          -- 'Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'
    IsEnabled BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Application logs (for log viewer)
CREATE TABLE onedrive.ApplicationLogs (
    Id BIGSERIAL PRIMARY KEY,
    AccountId TEXT,                         -- NULL for global logs
    LogLevel TEXT NOT NULL,
    Timestamp TIMESTAMP NOT NULL DEFAULT NOW(),
    Message TEXT,
    Exception TEXT,
    SourceContext TEXT,                     -- Logger name/category
    Properties JSONB,                       -- Structured log properties
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Index for log viewer paging
CREATE INDEX idx_applicationlogs_accountid_timestamp ON onedrive.ApplicationLogs(AccountId, Timestamp DESC);
CREATE INDEX idx_applicationlogs_loglevel ON onedrive.ApplicationLogs(LogLevel);
```

**Hashing Strategy for GDPR Compliance**
- **Email Hashing**: `HashedEmail = SHA256(email.ToLower())` - used for unique account lookup
- **Account ID Hashing**: `Id (AccountId) = SHA256(microsoftAccountId + createdAtTicks)` where `microsoftAccountId` is the unique identifier from OAuth token claims (e.g., `oid` claim)
- **No PII Storage**: Neither plaintext email nor Microsoft account ID stored in database
- **Display Names**: Store user-provided display name (non-PII) or allow user to set custom nickname
- **Secure Storage Mapping**: If email display needed, store in platform-specific secure storage (encrypted) with reference key in database
- **Hash Verification**: On authentication, re-hash incoming OAuth claims to match against database records
- **GDPR Right to Erasure**: Deleting account record removes all associated data; secure storage cleared separately

### 3. Authentication Feature Slice

**Flow**
1. User launches app → no accounts found
2. "Add Account" button → OAuth consent screen (browser)
3. User authorizes → token captured and stored securely
4. Account added to database with hashed ID and default settings
5. Next launch → account list shown

**Components**
- `AuthenticationService`: MSAL + token refresh logic
- `ISecureTokenStorage`: Cross-platform storage abstraction
- `AccountRepository`: Read/write accounts to database
- `AddAccountViewModel` (ReactiveUI): Reactive properties for auth state
- `AddAccountView` (AvaloniaUI): Login workflow UI

### 4. Account Management Feature Slice

**Flow**
1. Subsequent launches show account list (left sidebar)
2. Select account → shows folder list for that account (initially empty)
3. "Start Sync" button initiates **bidirectional sync**
4. **"Edit Account" option** on home screen → settings dialog:
   - **Home Sync Directory**: User-configurable local path for synced files
   - **Concurrent Downloads**: Slider/input (1-20, default 5)
   - **Concurrent Uploads**: Slider/input (1-20, default 5)
   - **Enable Debug Logging**: Toggle for per-account debug logs
   - Disconnect account option

**Components**
- `AccountManagementService`: CRUD operations on Accounts
- `AccountRepository`: EF Core access to Accounts table
- `AccountListViewModel`: Reactive properties for account selection
- `AccountListView`: Sidebar with account list + "Add Account" button
- `EditAccountViewModel`: Reactive properties for account settings
- `EditAccountView`: Settings dialog with sync directory picker, concurrency sliders, debug toggle

### 5. FileSync Feature Slice (Two-Way Sync)

**Flow**
1. User selects account
2. First sync: fetch root folder structure, populate FileSystemItems table
3. Display folder tree with IsSelected checkboxes
4. User selects folders to sync
5. "Start Sync" button: triggers **bidirectional sync** of selected items
   - **Download**: Remote changes → Local (using delta token)
   - **Upload**: Local changes → Remote (detect via file system watcher + hash comparison)
6. Background scheduler: periodically checks for **both** remote and local changes

**Components**
- `FileSyncService`: Orchestrates bidirectional sync workflow
- `DeltaSyncService`: Manages delta tokens, fetches incremental remote changes
- `LocalChangeDetectionService`: Monitors local file system for changes (FileSystemWatcher)
- `GraphApiClient`: Kiota-generated client for Microsoft Graph API
- `FileSystemRepository`: Read/write FileSystemItems
- `LocalFileOperationService`: Copy/delete/rename local files
- `RemoteFileOperationService`: Upload/delete/rename remote files via Graph API
- `DeltaTokenRepository`: Manage delta tokens per account/drive
- `ConcurrentDownloadQueue`: Queue manager for parallel downloads (configurable limit)
- `ConcurrentUploadQueue`: Queue manager for parallel uploads (configurable limit)
- `FolderSelectionViewModel` (ReactiveUI): Reactive tree with selection state
- `FolderSelectionView`: TreeView with checkboxes for folder selection
- `SyncStatusViewModel`: Progress indicator, sync status messages, upload/download counters

**Bidirectional Sync Algorithm**
```
For each selected folder in account:
  
  // STEP 1: Download (Remote → Local)
  1. Call Graph API with saved delta token
  2. Iterate remote changes:
     a. Compare RemoteHash vs LocalHash
     b. If different:
        - Queue for download (respecting MaxConcurrentDownloads)
        - Update FileSystemItems table
     c. If conflict detected (both modified):
        - Add to ConflictLogs for user resolution
  3. Save new delta token
  
  // STEP 2: Upload (Local → Remote)
  4. Scan local file system for changes (FileSystemWatcher events + hash comparison)
  5. For each local change:
     a. Compare LocalHash vs RemoteHash (from last sync)
     b. If different:
        - Queue for upload (respecting MaxConcurrentUploads)
        - Update FileSystemItems table
     c. If conflict detected (both modified):
        - Add to ConflictLogs for user resolution
  
  // STEP 3: Process Queues
  6. Process download queue with MaxConcurrentDownloads workers
  7. Process upload queue with MaxConcurrentUploads workers
  8. Update SyncHistory with upload/download counts
```

**Concurrent Upload/Download**
- Use `SemaphoreSlim` to limit concurrent operations
- Configurable per account (default 5 for both uploads and downloads)
- Progress reporting for each queued operation
- Retry logic with exponential backoff for transient failures

### 6. Conflict Resolution Feature Slice

**Flow**
1. During sync, conflicts detected → added to ConflictLogs
2. UI shows conflict dialog with options:
   - Keep Local (upload local version)
   - Keep Remote (download remote version)
   - Keep Both (save local with "_local" suffix, keep remote)
   - Ignore for Now (retry on next sync)
3. User selects action → applied and logged

**Components**
- `ConflictDetectionService`: Compare timestamps and hashes during sync
- `ConflictResolutionService`: Apply user's chosen resolution
- `ConflictRepository`: Query/update ConflictLogs
- `ConflictResolutionViewModel` (ReactiveUI): Dialog state, user choice
- `ConflictResolutionView`: Dialog with resolution options

### 7. Scheduling Feature Slice

**Flow**
1. User configures sync interval in settings (default: 5 minutes)
2. Background service runs on schedule
3. Checks for **both remote and local changes** using delta tokens + file system monitoring
4. Queues conflicts if found
5. Notifies UI of new conflicts

**Components**
- `SyncSchedulerService`: Timer-based background scheduler
- `BackgroundSyncWorker`: Hosted service running in background
- `ScheduleConfigurationViewModel`: Reactive properties for interval setting
- `ScheduleConfigurationView`: UI for configuring sync frequency

**Scheduler Implementation**
```csharp
// Pseudo-code
public class BackgroundSyncWorker : BackgroundService
{
    private Timer _timer;
    private int _intervalSeconds = 300; // 5 min default
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _timer = new Timer(async _ => await SyncAsync(), null, 
                          TimeSpan.Zero, TimeSpan.FromSeconds(_intervalSeconds));
    }
    
    private async Task SyncAsync()
    {
        foreach (var account in _accountRepo.GetActiveAccounts())
        {
            var selectedFolders = _fileSystemRepo.GetSelectedFolders(account.Id);
            foreach (var folder in selectedFolders)
            {
                // Bidirectional sync
                await _fileSyncService.SyncFolderBidirectionalAsync(account.Id, folder.Id);
            }
        }
    }
}
```

### 8. Telemetry & Logging Feature Slice

**OpenTelemetry Configuration**
- Export traces to PostgreSQL database (`onedrive` schema) as primary destination
- Fallback to local file (`{AppDataFolder}/logs/traces.json`) if database unavailable
- Metrics: sync duration, items uploaded/downloaded, conflict count, API call latency
- Logs: structured logging via `ILogger<T>` with semantic properties, stored in `ApplicationLogs` table

**Per-Account Diagnostic Logging**
- `DiagnosticSettings` table: log level per account
- `EnableDebugLogging` flag in `Accounts` table for quick toggle
- Factory: `ILoggerProvider` respects per-account log level
- UI: Settings dialog (Edit Account) allows user to enable debug logging

**Components**
- `OpenTelemetryConfiguration`: Setup traces, metrics, logs
- `DatabaseTraceExporter`: Custom exporter writing to database
- `DatabaseLogProvider`: Custom log provider writing to `ApplicationLogs` table
- `DiagnosticSettingsService`: CRUD operations on DiagnosticSettings
- `StructuredLoggingExtensions`: Helper methods for semantic logging

### 9. Log Viewer Feature Slice

**Flow**
1. User clicks **"View Logs"** from home screen menu
2. Dialog/window opens with account selector dropdown
3. User selects account (or "All Accounts" option)
4. Logs displayed in paged table (default 100 rows per page)
5. User can filter by log level, search by message, navigate pages

**Components**
- `LogViewerService`: Query `ApplicationLogs` table with paging and filtering
- `LogViewerRepository`: EF Core access to `ApplicationLogs` table
- `LogViewerViewModel` (ReactiveUI): Reactive properties for paging, filtering, account selection
- `LogViewerView` (AvaloniaUI): Paged table with filters and navigation

**Paging Implementation**
```csharp
// Pseudo-code
public class LogViewerService
{
    public async Task<PagedResult<ApplicationLog>> GetLogsAsync(
        string accountId, 
        int pageNumber, 
        int pageSize = 100,
        string logLevel = null,
        string searchTerm = null)
    {
        var query = _dbContext.ApplicationLogs
            .Where(log => accountId == null || log.AccountId == accountId)
            .OrderByDescending(log => log.Timestamp);
        
        if (logLevel != null)
            query = query.Where(log => log.LogLevel == logLevel);
        
        if (searchTerm != null)
            query = query.Where(log => log.Message.Contains(searchTerm));
        
        var total = await query.CountAsync();
        var logs = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new PagedResult<ApplicationLog>
        {
            Items = logs,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
```

## Implementation Roadmap

> **IMPORTANT**: Each checkbox below represents a **single, independent task** that should be implemented and tested separately. Do NOT combine multiple tasks into one implementation. Each task should result in a focused, reviewable pull request.

---

### Phase 1: Foundation (Layers & DI)

**Purpose**: Establish the foundational architecture, dependency injection, and database infrastructure.

**Task 1.1**: Set up project structure
- [ ] Create `Features/` folder structure for all feature slices
- [ ] Create `Common/` folder for shared models, extensions, constants
- [ ] Create `Infrastructure/` folder for cross-cutting concerns

**Task 1.2**: Configure Dependency Injection
- [ ] Add `Microsoft.Extensions.DependencyInjection` NuGet package
- [ ] Create `AppModule.cs` for DI container registration
- [ ] Configure service lifetimes (Singleton, Scoped, Transient)

**Task 1.3**: Add core NuGet packages
- [ ] Add EF Core and PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- [ ] Add MSAL for OAuth authentication
- [ ] Add Kiota abstractions for Graph API

**Task 1.4**: Add UI NuGet packages
- [ ] Add AvaloniaUI core package
- [ ] Add Avalonia.ReactiveUI for MVVM integration
- [ ] Add ReactiveUI framework

**Task 1.5**: Add observability NuGet packages
- [ ] Add OpenTelemetry core packages
- [ ] Add OpenTelemetry exporters (InMemory for testing)
- [ ] Add Serilog and PostgreSQL sink

**Task 1.6**: Create DbContext with schema configuration
- [ ] Create `OneDriveSyncDbContext` class
- [ ] Configure `onedrive` schema in `OnModelCreating`
- [ ] Add connection string to `appsettings.json`

**Task 1.7**: Create initial database migrations
- [ ] Add migration for `Accounts` table with hashing fields
- [ ] Add migration for `DeltaTokens` table
- [ ] Add migration for `FileSystemItems` table with hash tracking

**Task 1.8**: Create remaining database migrations
- [ ] Add migration for `ConflictLogs` table
- [ ] Add migration for `SyncHistory` table
- [ ] Add migration for `DiagnosticSettings` table

**Task 1.9**: Create logging table migration
- [ ] Add migration for `ApplicationLogs` table with indexes
- [ ] Verify all foreign key constraints are correctly configured

**Task 1.10**: Implement `ISecureTokenStorage` abstraction and factory
- [ ] Define `ISecureTokenStorage` interface in Infrastructure layer
- [ ] Create `SecureTokenStorageFactory` for platform detection
- [ ] Implement `WindowsSecureTokenStorage` (DPAPI-based)
- [ ] Implement `MacOSSecureTokenStorage` (Keychain-based)
- [ ] Implement `LinuxSecureTokenStorage` (SecretService D-Bus)
- [ ] Implement `AesSecureTokenStorage` (AES-256 encrypted fallback)
- [ ] Create factory pattern for platform-specific selection

**Task 1.11**: Implement unit tests for secure storage
- [ ] Create base test class with common scenarios (SecureTokenStorageTestsBase)
- [ ] Add unit tests for Windows DPAPI storage with encryption/decryption verification
- [ ] Add unit tests for AES-256 encrypted storage with integrity checks
- [ ] Add unit tests for SecureTokenStorageFactory platform detection
- [ ] Verify all 71 tests pass with Xunit + Shouldly
- [ ] Validate tamper detection and error handling

---

### Phase 2: Authentication & Accounts

**Purpose**: Implement OAuth authentication, account management, and secure token handling.

**Task 2.1**: Implement AuthenticationService (MSAL integration)
- [ ] Create `AuthenticationService` class with MSAL integration
- [ ] Implement OAuth Device Code Flow for cross-platform support
- [ ] Add unit tests for authentication flow

**Task 2.2**: Implement token refresh logic
- [ ] Implement proactive token refresh (5 minutes before expiry)
- [ ] Add exponential backoff for failed refresh attempts
- [ ] Add unit tests for refresh timing and retry logic

**Task 2.3**: Implement token storage integration
- [ ] Integrate `ISecureTokenStorage` into `AuthenticationService`
- [ ] Implement token save/retrieve/delete operations
- [ ] Add unit tests mocking `ISecureTokenStorage`

**Task 2.4**: Implement hashing service
- [ ] Create `HashingService` for SHA256 hashing
- [ ] Implement email hashing (case-insensitive)
- [ ] Implement account ID hashing with salt (createdAtTicks)
- [ ] Add unit tests with various input scenarios

**Task 2.5**: Create Account domain models
- [ ] Create `Account` entity class with all properties
- [ ] Create `AccountSettings` value object
- [ ] Add validation rules for account data

**Task 2.6**: Implement AccountRepository
- [ ] Create `AccountRepository` class with EF Core
- [ ] Implement CRUD operations (Create, Read, Update, Delete)
- [ ] Add unit tests mocking DbContext

**Task 2.7**: Implement AccountManagementService
- [ ] Create `AccountManagementService` orchestration layer
- [ ] Implement account creation with hashing
- [ ] Implement account retrieval by hashed ID
- [ ] Add unit tests with repository mocks

**Task 2.8**: Implement account update logic
- [ ] Implement update for HomeSyncDirectory, MaxConcurrent settings
- [ ] Implement debug logging toggle
- [ ] Add unit tests for update scenarios

**Task 2.9**: Implement account deletion with GDPR compliance
- [ ] Implement cascade delete for all related data
- [ ] Implement secure storage cleanup
- [ ] Add unit tests verifying complete data removal

**Task 2.10**: Build Add Account ViewModel
- [ ] Create `AddAccountViewModel` with ReactiveUI
- [ ] Implement reactive properties for auth state
- [ ] Add reactive commands for authentication flow
- [ ] Add unit tests for ViewModel state transitions

**Task 2.11**: Build Add Account View (UI)
- [ ] Create `AddAccountView.axaml` with AvaloniaUI
- [ ] Implement OAuth browser launch flow
- [ ] Bind View to ViewModel
- [ ] Test UI flow manually

**Task 2.12**: Build Account List ViewModel
- [ ] Create `AccountListViewModel` with ReactiveUI
- [ ] Implement reactive collection for accounts
- [ ] Implement account selection logic
- [ ] Add unit tests for list management

**Task 2.13**: Build Account List View (UI)
- [ ] Create `AccountListView.axaml` for sidebar
- [ ] Implement account list display with "Add Account" button
- [ ] Bind View to ViewModel
- [ ] Test UI flow manually

**Task 2.14**: Build Edit Account ViewModel
- [ ] Create `EditAccountViewModel` with ReactiveUI
- [ ] Implement reactive properties for settings (sync directory, concurrency, debug)
- [ ] Add validation logic for input fields
- [ ] Add unit tests for validation and state management

**Task 2.15**: Build Edit Account View (UI)
- [ ] Create `EditAccountView.axaml` settings dialog
- [ ] Implement folder picker for HomeSyncDirectory
- [ ] Implement sliders for concurrent operations (1-20)
- [ ] Implement debug logging toggle
- [ ] Bind View to ViewModel

**Task 2.16**: Implement end-to-end authentication flow
- [ ] Integrate all components (Auth → Repository → UI)
- [ ] Test: Launch → Add Account → Authenticate → Account appears
- [ ] Write BDD scenario for authentication flow

**Task 2.17**: Implement end-to-end account editing flow
- [ ] Integrate Edit Account UI with backend services
- [ ] Test: Edit Account → Change settings → Verify persistence
- [ ] Write BDD scenario for account editing

---

### Phase 3: File Sync & Delta (Two-Way)

**Purpose**: Implement bidirectional file synchronization with Microsoft Graph API.

**Task 3.1**: Generate Kiota client for Microsoft Graph API
- [ ] Install Kiota CLI tool
- [ ] Generate Graph API client code from OpenAPI spec
- [ ] Add generated code to `Infrastructure/GraphApi/` folder

**Task 3.2**: Configure Graph API client
- [ ] Create `GraphApiClientFactory` for authenticated client instances
- [ ] Configure authentication provider integration
- [ ] Add unit tests mocking Graph API responses

**Task 3.3**: Create FileSystemItem domain models
- [ ] Create `FileSystemItem` entity class with hash fields
- [ ] Create related value objects (SyncStatus, LastSyncDirection)
- [ ] Add validation rules

**Task 3.4**: Implement FileSystemRepository
- [ ] Create `FileSystemRepository` class with EF Core
- [ ] Implement CRUD operations for FileSystemItems
- [ ] Implement query for selected folders by AccountId
- [ ] Add unit tests mocking DbContext

**Task 3.5**: Create DeltaToken domain models
- [ ] Create `DeltaToken` entity class
- [ ] Add validation rules

**Task 3.6**: Implement DeltaTokenRepository
- [ ] Create `DeltaTokenRepository` class with EF Core
- [ ] Implement save/retrieve operations per account and drive
- [ ] Add unit tests mocking DbContext

**Task 3.7**: Implement DeltaSyncService (remote change detection)
- [ ] Create `DeltaSyncService` class
- [ ] Implement Graph API delta query with saved token
- [ ] Implement change parsing (add/update/delete)
- [ ] Add unit tests mocking Graph API client

**Task 3.8**: Implement remote change mapping
- [ ] Map Graph API responses to FileSystemItem entities
- [ ] Implement hash comparison for change detection
- [ ] Add unit tests for mapping logic

**Task 3.9**: Implement delta token persistence
- [ ] Update DeltaToken after successful sync
- [ ] Handle initial sync (no delta token)
- [ ] Add unit tests for token lifecycle

**Task 3.10**: Implement LocalChangeDetectionService
- [ ] Create `LocalChangeDetectionService` class
- [ ] Implement FileSystemWatcher for file events
- [ ] Implement debouncing for multiple rapid events
- [ ] Add unit tests with file system mocks

**Task 3.11**: Implement local hash computation
- [ ] Implement file hash calculation using `System.IO.Hashing` (XXHash)
- [ ] Implement hash comparison with cached values
- [ ] Add unit tests with various file scenarios

**Task 3.12**: Implement local change queuing
- [ ] Queue detected local changes for upload
- [ ] Track change type (add, modify, delete, rename)
- [ ] Add unit tests for queue management

**Task 3.13**: Implement RemoteFileOperationService (upload)
- [ ] Create `RemoteFileOperationService` class
- [ ] Implement file upload via Graph API
- [ ] Implement multipart upload for large files (> 4MB)
- [ ] Add unit tests mocking Graph API client

**Task 3.14**: Implement remote delete and rename operations
- [ ] Implement remote file deletion via Graph API
- [ ] Implement remote file rename via Graph API
- [ ] Add unit tests for each operation

**Task 3.15**: Implement LocalFileOperationService (download)
- [ ] Create `LocalFileOperationService` class
- [ ] Implement file download to local directory
- [ ] Implement directory creation for nested paths
- [ ] Add unit tests with file system mocks

**Task 3.16**: Implement local delete and rename operations
- [ ] Implement local file deletion with error handling
- [ ] Implement local file rename with error handling
- [ ] Add unit tests for each operation

**Task 3.17**: Implement ConcurrentDownloadQueue
- [ ] Create `ConcurrentDownloadQueue` class
- [ ] Implement semaphore-based concurrency limiting
- [ ] Implement FIFO queue processing
- [ ] Add unit tests verifying concurrency limits

**Task 3.18**: Implement download retry logic
- [ ] Implement exponential backoff for failed downloads
- [ ] Implement progress tracking for each download
- [ ] Add unit tests for retry scenarios

**Task 3.19**: Implement ConcurrentUploadQueue
- [ ] Create `ConcurrentUploadQueue` class
- [ ] Implement semaphore-based concurrency limiting
- [ ] Implement FIFO queue processing
- [ ] Add unit tests verifying concurrency limits

**Task 3.20**: Implement upload retry logic
- [ ] Implement exponential backoff for failed uploads
- [ ] Implement progress tracking for each upload
- [ ] Add unit tests for retry scenarios

**Task 3.21**: Implement FileSyncService orchestration
- [ ] Create `FileSyncService` class
- [ ] Orchestrate bidirectional sync workflow
- [ ] Integrate DeltaSync (download) and LocalChange (upload) services
- [ ] Add unit tests for orchestration logic

**Task 3.22**: Build Folder Selection ViewModel
- [ ] Create `FolderSelectionViewModel` with ReactiveUI
- [ ] Implement tree structure with reactive properties
- [ ] Implement IsSelected propagation (parent/child checkboxes)
- [ ] Add unit tests for tree state management

**Task 3.23**: Build Folder Selection View (UI)
- [ ] Create `FolderSelectionView.axaml` with TreeView
- [ ] Implement checkboxes for folder selection
- [ ] Bind View to ViewModel
- [ ] Test UI manually

**Task 3.24**: Build Sync Status ViewModel
- [ ] Create `SyncStatusViewModel` with ReactiveUI
- [ ] Implement reactive properties for progress, upload/download counts
- [ ] Implement status message updates
- [ ] Add unit tests for status updates

**Task 3.25**: Build Sync Status View (UI)
- [ ] Create `SyncStatusView.axaml` for progress display
- [ ] Implement progress bars and counters (↑ uploads, ↓ downloads)
- [ ] Bind View to ViewModel
- [ ] Test UI manually

**Task 3.26**: Implement "Start Sync" command
- [ ] Add "Start Sync" button to main UI
- [ ] Wire button to FileSyncService
- [ ] Implement sync trigger for selected folders
- [ ] Test manual sync flow

**Task 3.27**: Implement end-to-end bidirectional sync test
- [ ] Test: Download remote changes → verify local files created
- [ ] Test: Upload local changes → verify remote files created
- [ ] Write BDD scenario for bidirectional sync

---

### Phase 4: Conflict Resolution

**Purpose**: Detect and resolve sync conflicts with user input.

**Task 4.1**: Create Conflict domain models
- [ ] Create `ConflictLog` entity class
- [ ] Create `ConflictType` and `ResolutionAction` enums
- [ ] Add validation rules

**Task 4.2**: Implement ConflictRepository
- [ ] Create `ConflictRepository` class with EF Core
- [ ] Implement CRUD operations for ConflictLogs
- [ ] Implement query for unresolved conflicts by AccountId
- [ ] Add unit tests mocking DbContext

**Task 4.3**: Implement ConflictDetectionService
- [ ] Create `ConflictDetectionService` class
- [ ] Implement timestamp comparison logic
- [ ] Implement hash comparison logic
- [ ] Determine conflict type (local_newer, remote_newer, both_modified)
- [ ] Add unit tests for various conflict scenarios

**Task 4.4**: Integrate conflict detection into sync workflow
- [ ] Add conflict detection to FileSyncService
- [ ] Log conflicts to ConflictLogs table
- [ ] Skip conflicted files in automatic sync
- [ ] Add unit tests for integration

**Task 4.5**: Implement ConflictResolutionService
- [ ] Create `ConflictResolutionService` class
- [ ] Implement "Keep Local" resolution (upload local version)
- [ ] Add unit tests for Keep Local scenario

**Task 4.6**: Implement additional resolution strategies
- [ ] Implement "Keep Remote" resolution (download remote version)
- [ ] Implement "Keep Both" resolution (rename local with "_local" suffix)
- [ ] Implement "Ignore for Now" (defer to next sync)
- [ ] Add unit tests for each strategy

**Task 4.7**: Build Conflict Resolution ViewModel
- [ ] Create `ConflictResolutionViewModel` with ReactiveUI
- [ ] Implement reactive properties for conflict details
- [ ] Implement resolution action commands
- [ ] Add unit tests for ViewModel logic

**Task 4.8**: Build Conflict Resolution View (UI)
- [ ] Create `ConflictResolutionView.axaml` dialog
- [ ] Display conflict details (file name, timestamps, paths)
- [ ] Implement resolution buttons (Keep Local, Keep Remote, Keep Both, Ignore)
- [ ] Bind View to ViewModel

**Task 4.9**: Implement conflict notification flow
- [ ] Trigger conflict dialog when conflicts detected
- [ ] Display unresolved conflict count in UI
- [ ] Add unit tests for notification logic

**Task 4.10**: Implement end-to-end conflict resolution test
- [ ] Test: Trigger conflict → display dialog → resolve → verify action
- [ ] Write BDD scenario for conflict resolution

---

### Phase 5: Scheduling & Background Sync

**Purpose**: Implement automatic periodic synchronization in the background.

**Task 5.1**: Implement BackgroundSyncWorker
- [ ] Create `BackgroundSyncWorker` class inheriting from `BackgroundService`
- [ ] Implement timer-based execution loop
- [ ] Handle cancellation token for graceful shutdown
- [ ] Add unit tests mocking sync service

**Task 5.2**: Implement SyncSchedulerService
- [ ] Create `SyncSchedulerService` class
- [ ] Implement configurable interval (default 5 minutes)
- [ ] Trigger bidirectional sync for all active accounts
- [ ] Add unit tests for scheduling logic

**Task 5.3**: Integrate scheduler with FileSyncService
- [ ] Call FileSyncService from BackgroundSyncWorker
- [ ] Pass SyncType="scheduled" to distinguish from manual sync
- [ ] Add unit tests for integration

**Task 5.4**: Implement SyncHistory tracking
- [ ] Create `SyncHistory` entity class
- [ ] Create `SyncHistoryRepository` with EF Core
- [ ] Log sync start, completion, and results
- [ ] Add unit tests for history logging

**Task 5.5**: Build Schedule Configuration ViewModel
- [ ] Create `ScheduleConfigurationViewModel` with ReactiveUI
- [ ] Implement reactive property for interval (seconds)
- [ ] Add validation for interval range
- [ ] Add unit tests for ViewModel

**Task 5.6**: Build Schedule Configuration View (UI)
- [ ] Create `ScheduleConfigurationView.axaml` in settings
- [ ] Implement slider/input for interval configuration
- [ ] Bind View to ViewModel
- [ ] Test UI manually

**Task 5.7**: Implement background sync notification
- [ ] Display sync indicator in UI when background sync runs
- [ ] Show last sync timestamp
- [ ] Add unit tests for notification logic

**Task 5.8**: Implement end-to-end background sync test
- [ ] Test: Configure interval → wait for scheduled sync → verify execution
- [ ] Write BDD scenario for background scheduling

---

### Phase 6: Telemetry & Diagnostics

**Purpose**: Implement OpenTelemetry for observability and per-account diagnostic logging.

**Task 6.1**: Configure OpenTelemetry traces
- [ ] Add OpenTelemetry configuration in `Program.cs`
- [ ] Configure trace collection for sync operations
- [ ] Add activity sources for instrumentation
- [ ] Test trace generation

**Task 6.2**: Configure OpenTelemetry metrics
- [ ] Configure metric collection (sync duration, items synced, conflicts)
- [ ] Add meters for custom metrics
- [ ] Test metric generation

**Task 6.3**: Implement DatabaseTraceExporter
- [ ] Create custom `DatabaseTraceExporter` class
- [ ] Export traces to PostgreSQL database (onedrive schema)
- [ ] Add unit tests mocking database writes

**Task 6.4**: Implement DatabaseLogProvider
- [ ] Create custom `ILoggerProvider` implementation
- [ ] Write logs to `ApplicationLogs` table
- [ ] Add structured properties (JSONB column)
- [ ] Add unit tests for log provider

**Task 6.5**: Implement fallback file-based exporter
- [ ] Create file-based trace exporter for cases where DB unavailable
- [ ] Write traces to `{AppDataFolder}/logs/traces.json`
- [ ] Add unit tests for file exporter

**Task 6.6**: Implement per-account diagnostic logging
- [ ] Create `DiagnosticSettings` entity and repository
- [ ] Implement per-account log level configuration
- [ ] Integrate with logging provider to respect account-specific levels
- [ ] Add unit tests for per-account filtering

**Task 6.7**: Integrate debug logging toggle in Edit Account UI
- [ ] Add debug logging toggle to `EditAccountViewModel`
- [ ] Wire toggle to DiagnosticSettings repository
- [ ] Update logging configuration dynamically
- [ ] Test toggle behavior

**Task 6.8**: Implement structured logging helpers
- [ ] Create `StructuredLoggingExtensions` class
- [ ] Add helper methods for semantic logging (with properties)
- [ ] Add unit tests for logging helpers

**Task 6.9**: Implement end-to-end telemetry test
- [ ] Test: Generate traces/metrics/logs → verify stored in database
- [ ] Test: Per-account debug logging filters correctly
- [ ] Write BDD scenario for diagnostic logging

---

### Phase 7: Log Viewer

**Purpose**: Implement UI for viewing and filtering application logs.

**Task 7.1**: Create LogViewer domain models
- [ ] Create `ApplicationLog` entity class (already defined in schema)
- [ ] Create `PagedResult<T>` generic model for paging
- [ ] Add validation rules

**Task 7.2**: Implement LogViewerRepository
- [ ] Create `LogViewerRepository` class with EF Core
- [ ] Implement paged query with filtering (accountId, logLevel, searchTerm)
- [ ] Implement sorting by timestamp descending
- [ ] Add unit tests mocking DbContext

**Task 7.3**: Implement LogViewerService
- [ ] Create `LogViewerService` orchestration layer
- [ ] Implement paging logic (default 100 rows per page)
- [ ] Implement filter application
- [ ] Add unit tests for service logic

**Task 7.4**: Build LogViewer ViewModel
- [ ] Create `LogViewerViewModel` with ReactiveUI
- [ ] Implement reactive properties for logs collection, filters, pagination
- [ ] Implement commands for page navigation
- [ ] Add unit tests for ViewModel state management

**Task 7.5**: Build LogViewer View (UI)
- [ ] Create `LogViewerView.axaml` dialog/window
- [ ] Implement account selector dropdown
- [ ] Implement log level filter dropdown
- [ ] Implement search text input
- [ ] Bind View to ViewModel

**Task 7.6**: Implement log table display
- [ ] Implement paged table/grid for log entries
- [ ] Display columns: Timestamp, Level, Message, Exception, Context
- [ ] Implement page navigation controls (Previous, Next, Page #)
- [ ] Test UI manually

**Task 7.7**: Add "View Logs" menu item to main screen
- [ ] Add "View Logs" button to home screen menu bar
- [ ] Wire button to open LogViewerView
- [ ] Test navigation flow

**Task 7.8**: Implement log export functionality (optional)
- [ ] Add "Export to CSV" button in log viewer
- [ ] Implement CSV export logic
- [ ] Test export functionality

**Task 7.9**: Implement end-to-end log viewer test
- [ ] Test: Open log viewer → select account → filter by level → verify results
- [ ] Test: Navigate pages → verify correct rows displayed
- [ ] Write BDD scenario for log viewing

---

### Phase 8: Refinement & Testing

**Purpose**: Comprehensive testing, cross-platform validation, and performance optimization.

**Task 8.1**: Write unit tests for AuthenticationService
- [ ] Test token refresh timing and retry logic
- [ ] Achieve 95% code coverage for AuthenticationService
- [ ] Use NSubstitute and Shouldly

**Task 8.2**: Write unit tests for all sync services
- [ ] Test DeltaSyncService, LocalChangeDetectionService, FileSyncService
- [ ] Test ConcurrentDownloadQueue and ConcurrentUploadQueue
- [ ] Achieve 95% code coverage for sync components

**Task 8.3**: Write unit tests for all repositories
- [ ] Test all repository CRUD operations
- [ ] Achieve 85% code coverage for repository layer
- [ ] Use in-memory database or mocks

**Task 8.4**: Write integration tests for database operations
- [ ] Use Testcontainers for real PostgreSQL instances
- [ ] Test migrations, foreign keys, indexes
- [ ] Test paging performance with large datasets

**Task 8.5**: Write integration tests for Graph API interactions
- [ ] Use mock Graph API server or recorded responses
- [ ] Test delta sync, upload, download, delete operations
- [ ] Test error handling and retries

**Task 8.6**: Write BDD scenarios for all features
- [ ] Implement SpecFlow feature files from plan
- [ ] Write step definitions for all scenarios
- [ ] Run and verify all BDD tests pass

**Task 8.7**: Perform Windows platform testing
- [ ] Test on Windows 10 and Windows 11
- [ ] Verify DPAPI secure storage works correctly
- [ ] Test UI rendering and responsiveness

**Task 8.8**: Perform macOS platform testing
- [ ] Test on macOS (latest 2 versions)
- [ ] Verify Keychain secure storage works correctly
- [ ] Test UI rendering and responsiveness

**Task 8.9**: Perform Linux platform testing
- [ ] Test on Ubuntu, Fedora, and Arch Linux
- [ ] Verify SecretService secure storage works correctly
- [ ] Test UI rendering and responsiveness

**Task 8.10**: Perform load testing for large file syncs
- [ ] Test sync of 1GB+ files
- [ ] Verify memory usage remains reasonable
- [ ] Test concurrent upload/download with large files

**Task 8.11**: Perform load testing for many small files
- [ ] Test sync of 10,000+ small files
- [ ] Verify performance is acceptable
- [ ] Test database query performance with large datasets

**Task 8.12**: Perform concurrency testing
- [ ] Test with concurrent limits of 1, 5, 10, 20
- [ ] Verify semaphore limits are respected
- [ ] Measure performance differences

**Task 8.13**: Perform PostgreSQL query optimization
- [ ] Analyze query plans for slow queries
- [ ] Add indexes where needed (beyond existing ones)
- [ ] Test paging performance with 1M+ log entries

**Task 8.14**: Perform security review
- [ ] Review token storage implementation on all platforms
- [ ] Review API usage for security best practices
- [ ] Verify GDPR compliance (hashing, data deletion)
- [ ] Perform penetration testing (if applicable)

**Task 8.15**: Perform code quality review
- [ ] Run static analysis tools (SonarQube, CodeQL)
- [ ] Address all critical and high-severity issues
- [ ] Verify code coverage meets targets (95% domain, 85% infrastructure)

**Task 8.16**: Create user documentation
- [ ] Write user guide for installation and setup
- [ ] Write troubleshooting guide
- [ ] Document configuration options

**Task 8.17**: Create developer documentation
- [ ] Document architecture and design decisions
- [ ] Document database schema and migrations
- [ ] Document build and deployment process

**Task 8.18**: Prepare for release
- [ ] Create release build configurations
- [ ] Test installer/deployment packages for all platforms
- [ ] Prepare release notes

## UI/UX Flow

### First Launch
```
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

### Subsequent Launches
```
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

### UI Layout (Updated)
```
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

**Home Screen Menu Items**
- **View Logs**: Opens log viewer dialog with account selector
- **Settings (⚙)**: Global app settings (sync interval, etc.)

**Per-Account Actions**
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

**Environment Variables**
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

**Framework & Tooling**
- **Testing Framework**: xUnit
- **Mocking**: NSubstitute for creating test doubles and verifying interactions
- **Assertions**: Shouldly for fluent, readable assertions

**Test Coverage by Component**

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

**Framework & Tooling**
- **BDD Framework**: SpecFlow (integrates with xUnit)
- **Assertions**: Shouldly for readable BDD assertions
- **Test Data**: Use in-memory PostgreSQL (TestContainers) or SQLite for test isolation
- **Mocking**: NSubstitute for external dependencies (Graph API)

**BDD Feature Files**

**Feature 1: Account Management**
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

**Feature 2: Folder Selection and Sync**
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

**Feature 3: Conflict Resolution**
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

**Feature 4: Background Scheduling**
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

**Feature 5: Log Viewer**
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

**Feature 6: Concurrent Operations**
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

**Feature 7: GDPR Compliance**
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

**Performance Tests**
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
