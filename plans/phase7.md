# Phase 7: Log Viewer

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

**Task 7.10**: Real-time statistics display

- [ ] During sync operations, display real-time stats in UI
- [ ] Show number of files uploaded, downloaded, conflicts detected, sync duration, and current upload/download speeds, ETA for completion.
