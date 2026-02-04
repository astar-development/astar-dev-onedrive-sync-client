# Phase 4: Conflict Resolution

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
- [ ] If one file has deleted and the other modified, treat as conflict.
- [ ] Handle edge cases (e.g., identical timestamps but different hashes)
- [ ] Log conflicts to ConflictLogs table
- [ ] Support cancellation token
- [ ] File casing conflicts should be detected during both upload and download phases of sync. Should be treated as a conflict in ConflictLogs.
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
