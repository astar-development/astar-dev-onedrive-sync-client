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

- [ ] Create `Account` entity class with all properties
- [ ] Create `AccountSettings` value object
- [ ] Add validation rules for account data

**Task 2.4**: Implement AccountRepository

- [ ] Create `AccountRepository` class with EF Core
- [ ] Implement CRUD operations (Create, Read, Update, Delete)
- [ ] Add unit tests mocking DbContext

**Task 2.5**: Implement AccountCreationService (NEW - orchestration layer)

- [ ] Create `AccountCreationService` orchestration layer
- [ ] Accept `AuthToken` from `AuthenticationService` as input
- [ ] Retrieve user profile from Graph API (email, account ID)
- [ ] Hash email and account ID using `HashingService`
- [ ] Save token to `ISecureTokenStorage` with hashed key
- [ ] Create account record in database via `AccountRepository`
- [ ] Return `Result<Account, AccountCreationError>` with functional error handling
- [ ] Add unit tests with repository and storage mocks
- [ ] **Separation of concerns**: This service bridges authentication and persistence

**Task 2.6**: Implement AccountManagementService (update/delete operations)

- [ ] Create `AccountManagementService` for account lifecycle management
- [ ] Implement account retrieval by hashed ID
- [ ] Implement update for HomeSyncDirectory, MaxConcurrent settings
- [ ] Implement debug logging toggle
- [ ] Add unit tests for update scenarios
- [ ] Implement per-account MaxBandwidthKBps configuration, rate-limiting stream wrapper, metered connection detection via platform APIs, and UI controls for pause/resume with bandwidth slider.

**Task 2.7**: Implement account deletion with GDPR compliance

- [ ] Implement cascade delete for all related data
- [ ] Implement secure storage cleanup
- [ ] Add unit tests verifying complete data removal

**Task 2.8**: Build Add Account ViewModel

- [ ] Create `AddAccountViewModel` with ReactiveUI
- [ ] Implement reactive properties for auth state
- [ ] Add reactive commands for authentication flow
- [ ] Handle `Result<AuthToken, AuthenticationError>` from `AuthenticationService`
- [ ] Display appropriate Toast notifications based on error type (TimedOut, NetworkError, etc.)
- [ ] Invoke `AccountCreationService` after successful authentication
- [ ] Add unit tests for ViewModel state transitions

**Task 2.9**: Build Add Account View (UI)

- [ ] Create `AddAccountView.axaml` with AvaloniaUI
- [ ] Implement OAuth browser launch flow
- [ ] Bind View to ViewModel
- [ ] Test UI flow manually

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
