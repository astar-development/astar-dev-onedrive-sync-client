# Phase 2: Authentication & Accounts

**Purpose**: Implement OAuth authentication, account management, and secure token handling.

**Task 2.1**: Implement AuthenticationService (MSAL integration)

- [ ] Create `AuthenticationService` class with MSAL integration
- [ ] Implement OAuth Device Code Flow for cross-platform support
- [ ] Use CancellationTokenSource for timeout handling (30 seconds)
- [ ] OperationCanceledException should be caught and a user-friendly message "Login timed out. Please try again." should be displayed via a Toast notification that auto dismisses after 5 seconds.
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
- [ ] Implement per-account MaxBandwidthKBps configuration, rate-limiting stream wrapper, metered connection detection via platform APIs, and UI controls for pause/resume with bandwidth slider.

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
