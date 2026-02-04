# Phase 5: Scheduling & Background Sync

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
