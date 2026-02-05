# Phase 3: File Sync & Delta (Two-Way)

**Purpose**: Implement bidirectional file synchronization with Microsoft Graph API.

**Task 3.1**: ✅ Add Microsoft Graph SDK (Done)

**Implementation Notes**:

- Added Microsoft.Graph v5.101.0 package (main SDK)
- Added Microsoft.Graph.Core v3.2.5 package (core functionality)  
- SDK provides GraphServiceClient for authenticated Graph API access
- Build verified successful (7.8s)

- [x] Install Microsoft Graph SDK packages
- [x] Verify build succeeds with new dependencies

**Task 3.2**: ✅ Configure Graph API Client Factory (Done)

**Implementation Notes**:

- Created GraphApiClientFactory with StaticTokenCredential for token-based authentication
- Implemented GraphApiClient wrapper around Microsoft Graph SDK's GraphServiceClient
- Registered factory as Singleton and GraphApiClient as Scoped service in DI container
- Replaced MockGraphApiClient with real Microsoft Graph SDK implementation
- Added Azure.Core v1.51.1 dependency for TokenCredential support
- Created comprehensive unit tests (4 tests) for GraphApiClientFactory
- All 667 tests passing (4 new tests added)
- Build verified successful

- [x] Create GraphApiClientFactory for authenticated client instances
- [x] Configure authentication provider integration with token credential
- [x] Add unit tests for factory (token validation, client creation)
- [x] Replace MockGraphApiClient with real SDK implementation

**Task 3.3**: ✅ Create FileSystemItem domain models (Done)

**Implementation Notes**:

- FileSystemItem entity already existed with all required properties
- Added validation rules to FileSystemItem:
  - Id cannot be null, empty, or whitespace
  - HashedAccountId cannot be null, empty, or whitespace
  - DriveItemId cannot be null, empty, or whitespace
- SyncStatus enum already existed (None, Synced, PendingUpload, PendingDownload, Conflict, Failed)
- SyncDirection enum already existed (None, Upload, Download, Bidirectional)
- Created 20 comprehensive unit tests in FileSystemItemShould.cs
- All 687 tests passing (20 new tests added for FileSystemItem validation)
- Build verified successful

- [x] Create `FileSystemItem` entity class with hash fields
- [x] Create related value objects (SyncStatus, LastSyncDirection)
- [x] Add validation rules

**Task 3.4**: ✅ Implement FileSystemRepository (Done)

**Implementation Notes**:

- Created IFileSystemRepository interface with 6 methods:
  - CreateAsync: Add new FileSystemItem to database
  - GetByIdAsync: Retrieve FileSystemItem by Id
  - GetAllByHashedAccountIdAsync: Get all items for an account
  - GetSelectedItemsByHashedAccountIdAsync: Get only selected items for an account
  - UpdateAsync: Update existing FileSystemItem
  - DeleteAsync: Remove FileSystemItem by Id
- Created FileSystemRepository implementation using EF Core:
  - Uses OneDriveSyncDbContext for database operations
  - Implements AsNoTracking for read queries (performance optimization)
  - Uses SaveChangesAsync for persistence
- Created 7 comprehensive integration tests using in-memory database:
  - CreateFileSystemItemAndPersistToDatabase
  - GetFileSystemItemByIdReturnsItemWhenExists
  - GetFileSystemItemByIdReturnsNullWhenNotExists
  - GetSelectedItemsByHashedAccountIdReturnsOnlySelectedItems
  - UpdateFileSystemItemPersistsChangesToDatabase
  - DeleteFileSystemItemRemovesFromDatabase
  - GetAllItemsByHashedAccountIdReturnsAllAccountItems
- All tests set IsFolder property explicitly (required by EF configuration)
- All 674 tests passing (7 new repository tests added)
- Build verified successful

- [x] Create `FileSystemRepository` class with EF Core
- [x] Implement CRUD operations for FileSystemItems
- [x] Implement query for selected folders by AccountId
- [x] Add unit tests mocking DbContext

**Task 3.5**: ✅ Create DeltaToken domain models (Done)

**Implementation Notes**:

- DeltaToken entity already existed with required properties
- Added validation rules to DeltaToken:
  - Id cannot be null, empty, or whitespace
  - HashedAccountId cannot be null, empty, or whitespace
  - DriveName cannot be null, empty, or whitespace
- Token and LastSyncAt remain nullable (optional fields)
- Created 18 comprehensive unit tests in DeltaTokenShould.cs
- All validation tests passing
- Build verified successful

- [x] Create `DeltaToken` entity class
- [x] Add validation rules

**Task 3.6**: ✅ Implement DeltaTokenRepository (Done)

**Implementation Notes**:

- Created IDeltaTokenRepository interface with 4 methods:
  - GetByAccountAndDriveAsync: Retrieve token for specific account and drive
  - GetAllByAccountAsync: Get all tokens for an account
  - SaveAsync: Create or update delta token (upsert logic)
  - DeleteAsync: Remove delta token by Id
- Created DeltaTokenRepository implementation using EF Core:
  - Uses OneDriveSyncDbContext for database operations
  - Implements AsNoTracking for read queries
  - SaveAsync handles both insert and update scenarios
  - Uses SaveChangesAsync for persistence
- Created 9 comprehensive integration tests using in-memory database:
  - SaveNewDeltaTokenAndPersistToDatabase
  - GetByAccountAndDriveReturnsTokenWhenExists
  - GetByAccountAndDriveReturnsNullWhenNotExists
  - GetAllByAccountReturnsAllTokensForAccount
  - SaveUpdatesExistingDeltaToken
  - DeleteRemovesDeltaTokenFromDatabase
  - DeleteDoesNothingWhenTokenNotFound
  - SaveHandlesMultipleDrivesPerAccount
- All 719 tests passing (27 new tests added: 18 DeltaToken + 9 repository)
- Build verified successful

- [x] Create `DeltaTokenRepository` class with EF Core
- [x] Implement save/retrieve operations per account and drive
- [x] Add unit tests mocking DbContext

**Task 3.7**: Implement DeltaSyncService (remote change detection)

- [ ] Create `DeltaSyncService` class
- [ ] Implement Graph API delta query with saved token
- [ ] Implement change parsing (add/update/delete)
- [ ] Status Code 429 should have retry with exponential backoff according to Retry-After header. If no header is present, use default backoff strategy with random jitter.
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

- [ ] Use [Testably](https://github.com/Testably/Testably.Abstractions) for actual file system integration
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
- [ ] Upload should handle Status Code 429 with retry using exponential backoff according to Retry-After header. If no header is present, use default backoff strategy with random jitter.
- [ ] Upload should resume from last byte on transient failures.
- [ ] CancellationToken support for upload operations
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
- [ ] implement connectivity monitoring via System.Net.NetworkInformation, automatic queue pause on disconnect, change queuing while offline, auto-resume on reconnect, and offline status indicator in UI.
