# Phase 2: Authentication & Accounts

**Purpose**: Implement OAuth authentication, account management, and secure token handling.

**Task 2.1**: Implement AuthenticationService (MSAL integration) ✅

- [x] Create `AuthenticationService` class with MSAL integration
- [x] Create `AuthenticationError` discriminated union for error handling (Cancelled, TimedOut, NetworkError, ServiceError, UnexpectedError, ConfigurationError)
- [x] Create `AuthToken` value object with expiry tracking and proactive refresh detection
- [x] Implement OAuth Device Code Flow for cross-platform support
- [x] Use CancellationTokenSource for timeout handling (30 seconds)
- [x] Return `Result<AuthToken, AuthenticationError>` for functional error handling
- [x] Implement proactive token refresh using MSAL silent authentication (AcquireTokenSilent)
- [x] Add exponential backoff for failed refresh attempts (3 retries with jitter)
- [x] Integration with `ISecureTokenStorage` and `ILogger`
- [x] Add unit tests for authentication flow (mocking needs refinement due to MSAL sealed classes)
- [x] **Separation of concerns**: AuthenticationService handles ONLY authentication, does NOT create accounts or know about MS Graph

**Note on Task 2.1 Implementation:**
- ViewModels will handle Result matching and display Toast notifications (e.g., "Login timed out. Please try again.")
- MSAL manages refresh tokens internally; they are not exposed in `AuthToken`
- Token storage integration is deferred to `AccountCreationService` (post-authentication)

**Task 2.2**: Implement hashing service ✅

- [x] Create `HashingService` for SHA256 hashing
- [x] Implement email hashing (case-insensitive)
- [x] Implement account ID hashing with salt (createdAtTicks)
- [x] Add unit tests with various input scenarios (14 comprehensive tests)

**Task 2.3**: Create Account domain models

- [x] Create `Account` entity class with all properties
- [x] Create `AccountSettings` value object
- [x] Add validation rules for account data

**Task 2.4**: Implement AccountRepository

- [x] Create `AccountRepository` class with EF Core
- [x] Implement CRUD operations (Create, Read, Update, Delete)
- [x] Add unit tests mocking DbContext

**Task 2.5**: Implement AccountCreationService (NEW - orchestration layer)

- [x] Create `AccountCreationService` orchestration layer
- [x] Accept `AuthToken` from `AuthenticationService` as input
- [x] Retrieve user profile from Graph API (email, account ID)
- [x] Hash email and account ID using `HashingService`
- [x] Save token to `ISecureTokenStorage` with hashed key
- [x] Create account record in database via `AccountRepository`
- [x] Return `Result<Account, AccountCreationError>` with functional error handling
- [x] Add unit tests with repository and storage mocks
- [x] **Separation of concerns**: This service bridges authentication and persistence

**Task 2.6**: Implement AccountManagementService (update/delete operations) ✅

- [x] Create `AccountManagementService` for account lifecycle management
- [x] Create `AccountManagementError` discriminated union for error handling (AccountNotFound, RepositoryError, ValidationError, UnexpectedError)
- [x] Implement account retrieval by ID (GetAccountByIdAsync)
- [x] Implement update for HomeSyncDirectory (UpdateHomeSyncDirectoryAsync)
- [x] Implement update for MaxConcurrent settings (UpdateMaxConcurrentAsync)
- [x] Implement debug logging toggle (UpdateDebugLoggingAsync)
- [x] Add MaxBandwidthKBps nullable field to Account entity with database migration
- [x] Implement per-account MaxBandwidthKBps configuration (UpdateMaxBandwidthKBpsAsync)
- [x] Implement simple account deletion (DeleteAccountAsync) - full GDPR compliance deferred to Task 2.7
- [x] Add comprehensive unit tests for all scenarios (18 test methods)
- [x] Return `Result<T, AccountManagementError>` for functional error handling
- [x] Integration with `IAccountRepository` and `ILogger`
- [x] **Separation of concerns**: Handles ONLY account settings updates and simple deletion; GDPR-compliant deletion in Task 2.7

**Task 2.7**: Implement account deletion with GDPR compliance ✅

- [x] Implement cascade delete for all related data
- [x] Implement secure storage cleanup  
- [x] Add unit tests verifying complete data removal

**Task 2.8**: Build Add Account ViewModel ✅

- [x] Create `AddAccountViewModel` with ReactiveUI
- [x] Implement reactive properties for auth state (StatusMessage, ErrorMessage, IsAuthenticating, IsCreatingAccount, CreatedAccount)
- [x] Add reactive command for authentication flow (AuthenticateCommand using ReactiveCommand.CreateFromTask)
- [x] Handle `Result<AuthToken, AuthenticationError>` from `AuthenticationService` using pattern matching
- [x] Map error types to user-friendly messages (6 authentication errors, 6 account creation errors)
- [x] Invoke `AccountCreationService` after successful authentication
- [x] Add unit tests for ViewModel state transitions (5 tests covering constructor validation, initial state, successful flow, and error scenarios)

**Task 2.9**: Build Add Account View (UI) ✅

- [x] Create `AddAccountView.axaml` with AvaloniaUI
- [x] Implement OAuth browser launch flow UI elements
- [x] Bind View to ViewModel
- [x] Create code-behind file with proper initialization
- [x] Build and test successful (all 646 tests passing)

**Implementation Notes:**
- Created UserControl with data binding to AddAccountViewModel
- Implemented status messages, error display, and progress indicators
- Used Avalonia converters for visibility binding (StringConverters.IsNotNullOrEmpty, ObjectConverters.IsNotNull)
- Button enables/disables based on IsAuthenticating state
- Success message displays created account details
- All reactive properties properly bound (StatusMessage, ErrorMessage, IsAuthenticating, IsCreatingAccount, CreatedAccount)
- Follows Avalonia MVVM best practices with Design.DataContext for designer support

**Task 2.10**: Build Account List ViewModel

- [ ] Create `AccountListViewModel` with ReactiveUI
- [ ] Implement reactive collection for accounts
- [ ] Implement account selection logic
- [ ] Add unit tests for list management

**Task 2.11**: Build Account List View (UI)

- [ ] Create `AccountListView.axaml` for sidebar
- [ ] Implement account list display with "Add Account" button
- [ ] Bind View to ViewModel
- [ ] Test UI flow manually

**Task 2.12**: Build Edit Account ViewModel

- [ ] Create `EditAccountViewModel` with ReactiveUI
- [ ] Implement reactive properties for settings (sync directory, concurrency, debug)
- [ ] Add validation logic for input fields
- [ ] Add unit tests for validation and state management

**Task 2.13**: Build Edit Account View (UI)

- [ ] Create `EditAccountView.axaml` settings dialog
- [ ] Implement folder picker for HomeSyncDirectory
- [ ] Implement sliders for concurrent operations (1-20)
- [ ] Implement debug logging toggle
- [ ] Bind View to ViewModel

**Task 2.14**: Implement end-to-end authentication flow

- [ ] Integrate all components (Auth → AccountCreation → Repository → UI)
- [ ] Test: Launch → Add Account → Authenticate → Account appears
- [ ] Write BDD scenario for authentication flow

**Task 2.15**: Implement end-to-end account editing flow

- [ ] Integrate Edit Account UI with backend services
- [ ] Test: Edit Account → Change settings → Verify persistence
- [ ] Write BDD scenario for account editing

---

## Architecture Notes

### Separation of Concerns

**AuthenticationService** (Task 2.1 ✅)
- **Responsibility**: OAuth authentication with Microsoft using Device Code Flow
- **Input**: None (starts fresh authentication)
- **Output**: `Result<AuthToken, AuthenticationError>` with access token and expiry
- **Does NOT**: Create accounts, store tokens, or call Graph API for user profile

**AccountCreationService** (Task 2.5 - NEW)
- **Responsibility**: Orchestrate post-authentication account creation
- **Input**: `AuthToken` from successful authentication
- **Dependencies**: `IGraphApiClient`, `IHashingService`, `IAccountRepository`, `ISecureTokenStorage`
- **Workflow**:
  1. Use token to fetch user profile from Graph API (email, account ID)
  2. Hash email and account ID via HashingService
  3. Store token in secure storage with hashed key
  4. Create account record in database
- **Output**: `Result<Account, AccountCreationError>`

**AccountManagementService** (Task 2.6)
- **Responsibility**: Account lifecycle operations (update settings, delete account)
- **Operations**: Retrieve, update HomeSyncDirectory, update concurrency settings, delete with GDPR compliance

### Error Handling Strategy

All services use `Result<TSuccess, TError>` from `AStar.Dev.Functional.Extensions`:
- **Success path**: `Result<T, E>.Ok(value)`
- **Error path**: `Result<T, E>.Error(reason)`
- **ViewModels**: Use `.Match()` or `.MatchAsync()` to handle both cases
- **UI**: Display Toast notifications based on specific error types (TimedOut, NetworkError, ServiceError, etc.)
