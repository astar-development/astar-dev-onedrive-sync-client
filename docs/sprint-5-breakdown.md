# Sprint 5: Database Persistence for Folder Selections

**Goal**: Persist folder selection state to the database so selections survive app restarts.

**Date**: January 6, 2026
**Status**: Ready to Start
**Approach**: Small incremental steps (8 steps like Sprint 3 & 4)

---

## Overview

Currently, folder selections (checked/unchecked/indeterminate) are stored only in memory. When the app restarts or an account is deselected and reselected, all selections are lost. This sprint adds database persistence so selections are saved and restored automatically.

**Key Features**:

- Save folder selections to database when user toggles checkboxes
- Load saved selections when account is selected
- Update selections when they change
- Clear selections when user clicks "Clear All" (database + memory)
- Handle edge cases: deleted folders, renamed folders, new folders

---

## Database Schema (Already Exists!)

Good news: The `SyncConfigurations` table already exists from Sprint 1:

```sql
CREATE TABLE SyncConfigurations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountId TEXT NOT NULL,
    FolderPath TEXT NOT NULL,
    IsSelected INTEGER NOT NULL,  -- Boolean: 1 = checked, 0 = unchecked
    LastModifiedUtc TEXT NOT NULL,
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId) ON DELETE CASCADE
);

CREATE INDEX IX_SyncConfigurations_AccountId_FolderPath 
    ON SyncConfigurations(AccountId, FolderPath);
```

**Note**: We only persist **checked** folders (not indeterminate). Indeterminate state is calculated from children.

---

## Sprint 5 Steps Breakdown

### Step 5.1: Create SyncConfiguration Model

- **File**: `src/AStarOneDriveClient/Models/SyncConfiguration.cs`
- **Purpose**: Model representing a persisted folder selection
- **Properties**: `Id`, `AccountId`, `FolderPath`, `IsSelected`, `LastModifiedUtc`
- **Tests**: Simple property validation tests

### Step 5.2: Update ISyncConfigurationRepository Interface

- **File**: `src/AStarOneDriveClient/Repositories/ISyncConfigurationRepository.cs` (already exists)
- **Purpose**: Add methods for folder selection persistence
- **New Methods**:
  - `GetSelectionsAsync(accountId)` - Get all selected folders for an account
  - `SaveSelectionsAsync(accountId, folderPaths)` - Replace all selections for an account
  - `AddSelectionAsync(selection)` - Add a single selection
  - `RemoveSelectionAsync(accountId, folderPath)` - Remove a single selection
  - `ClearSelectionsAsync(accountId)` - Remove all selections for an account
- **Tests**: Interface doesn't need tests (implementations do)

### Step 5.3: Implement SyncConfigurationRepository Methods

- **File**: `src/AStarOneDriveClient/Repositories/SyncConfigurationRepository.cs` (already exists)
- **Purpose**: Implement the new persistence methods
- **Database Operations**: CRUD for `SyncConfigurations` table
- **Tests**: 15+ tests covering:
  - Save selections (new folders)
  - Update selections (existing folders)
  - Remove selections
  - Clear all selections
  - Get selections for account
  - Handle empty results
  - Verify foreign key constraints

### Step 5.4: Update ISyncSelectionService Interface

- **File**: `src/AStarOneDriveClient/Services/ISyncSelectionService.cs`
- **Purpose**: Add persistence capability to selection service
- **New Methods**:
  - `SaveSelectionsToDatabaseAsync(accountId, rootFolders)` - Persist current selections
  - `LoadSelectionsFromDatabaseAsync(accountId, rootFolders)` - Restore selections from DB
- **Tests**: Interface doesn't need tests (implementations do)

### Step 5.5: Implement SyncSelectionService Persistence Methods

- **File**: `src/AStarOneDriveClient/Services/SyncSelectionService.cs`
- **Purpose**: Implement database save/load logic
- **Logic**:
  - `SaveSelectionsToDatabaseAsync`: Collect all checked folders, save to repository
  - `LoadSelectionsFromDatabaseAsync`: Read from DB, apply selections to tree nodes
- **Dependencies**: Inject `ISyncConfigurationRepository` via constructor
- **Tests**: 10+ tests covering:
  - Save checked folders to database
  - Load selections and apply to tree
  - Handle folders not in database (new folders)
  - Handle folders in database but not in current tree (deleted folders)
  - Verify indeterminate state recalculated after loading

### Step 5.6: Update SyncTreeViewModel with Auto-Persistence

- **File**: `src/AStarOneDriveClient/ViewModels/SyncTreeViewModel.cs`
- **Purpose**: Automatically save/load selections when account changes
- **Changes**:
  - Inject `ISyncConfigurationRepository` in constructor
  - After `LoadFoldersAsync`: Call `LoadSelectionsFromDatabaseAsync`
  - After `ToggleSelection`: Call `SaveSelectionsToDatabaseAsync`
  - After `ClearSelections`: Call `ClearSelectionsAsync` on repository
- **Tests**: 8+ tests covering:
  - Selections loaded after folders load
  - Selections saved after toggle
  - Selections cleared in database when "Clear All" clicked
  - Multiple toggles batch correctly (avoid saving on every click - debounce?)

### Step 5.7: Update ServiceConfiguration DI Registration

- **File**: `src/AStarOneDriveClient/ServiceConfiguration.cs`
- **Purpose**: Wire up the repository in DI container
- **Changes**:
  - Ensure `ISyncConfigurationRepository` is registered (already done in Sprint 1)
  - Update `SyncSelectionService` registration to inject repository
  - Update `SyncTreeViewModel` registration to inject repository
- **Tests**: No new tests (verify existing DI tests still pass)

### Step 5.8: Integration Tests & Manual Testing

- **File**: `test/.../ViewModels/SyncTreeViewModelPersistenceIntegrationShould.cs`
- **Purpose**: End-to-end tests for persistence
- **Tests**: 6+ scenarios:
  - Select folders, restart ViewModel, verify selections restored
  - Switch accounts, verify each account's selections independent
  - Clear selections, verify database cleared
  - Add new folders (not in DB), verify they appear unchecked
  - Verify deleted folders in DB don't cause errors
- **Manual Testing**:
  - Select folders in UI
  - Close app completely
  - Reopen app, select same account
  - Verify selections restored ✅

---

## Technical Details

### Persistence Strategy

**When to Save**:

- After user toggles any checkbox (debounced if needed)
- After "Clear All" button clicked

**When to Load**:

- After folders loaded for an account (in `LoadFoldersAsync`)

**What to Save**:

- Only **checked** folders (not indeterminate)
- Store full OneDrive path (e.g., `/Documents/Work/Projects`)
- Store `LastModifiedUtc` for future conflict resolution

**What NOT to Save**:

- Indeterminate folders (calculated from children)
- Unchecked folders (absence = unchecked)

### Data Flow

``` text
User clicks checkbox
  ↓
ToggleSelectionCommand executes
  ↓
SyncSelectionService.SetSelection() updates tree in memory
  ↓
SyncTreeViewModel calls SaveSelectionsToDatabaseAsync()
  ↓
SyncSelectionService.SaveSelectionsToDatabaseAsync()
  ↓
Collects all checked folders (GetSelectedFolders)
  ↓
SyncConfigurationRepository.SaveSelectionsAsync()
  ↓
Database transaction: DELETE all for account, INSERT new selections
  ↓
Done ✅
```

### Loading Flow

``` text
User selects account
  ↓
SyncTreeViewModel.LoadFoldersAsync() executes
  ↓
Folders loaded from OneDrive API
  ↓
SyncTreeViewModel calls LoadSelectionsFromDatabaseAsync()
  ↓
SyncSelectionService.LoadSelectionsFromDatabaseAsync()
  ↓
SyncConfigurationRepository.GetSelectionsAsync(accountId)
  ↓
For each folder path in database:
  - Find matching OneDriveFolderNode in tree
  - Set SelectionState = Checked
  - Call SetSelection() to cascade to children
  ↓
Call UpdateParentStates() to calculate indeterminate
  ↓
Done ✅
```

---

## Edge Cases to Handle

1. **Folder deleted in OneDrive**: Selection exists in DB but folder doesn't exist anymore
   - **Solution**: Ignore silently (don't throw error)

2. **Folder renamed in OneDrive**: Path changed, selection orphaned
   - **Solution**: User must re-select (we use path as key, not ID for now)
   - **Future**: Use OneDrive DriveItem ID instead of path

3. **New folders added**: Not in database yet
   - **Solution**: Appear unchecked by default (expected behavior)

4. **Parent selected but children added later**: Parent in DB, new children not
   - **Solution**: Children should auto-check (cascading logic handles this)

5. **Database corrupted or empty**: App should still work
   - **Solution**: Gracefully handle empty results, don't crash

---

## Performance Considerations

### Debouncing Saves

If user clicks multiple checkboxes rapidly, we might save to DB on every click. Consider:

- **Option 1**: Debounce save by 500ms (wait for user to stop clicking)
- **Option 2**: Save immediately (database is fast enough)
- **Decision**: Start with immediate save, optimize later if needed

### Database Size

Each account might have 100-1000 selected folders:

- 100 folders × 20 bytes path = 2 KB per account
- 10 accounts × 2 KB = 20 KB total
- **Conclusion**: Size not a concern

### Load Performance

Loading 1000 selections and applying to tree:

- O(n) database read: ~10ms
- O(n × m) tree search: n=selections, m=tree size
  - Worst case: 1000 × 5000 = 5M ops (too slow!)
- **Optimization**: Build lookup dictionary `Dictionary<string, OneDriveFolderNode>` first
  - O(m) to build: 5000 nodes
  - O(n) to apply: 1000 lookups
  - Total: O(m + n) = ~15ms ✅

---

## Success Criteria

Sprint 5 is complete when:

- ✅ All 8 steps implemented
- ✅ All unit tests passing (40+ new tests)
- ✅ Integration tests passing (6+ tests)
- ✅ Manual test: Select folders → close app → reopen → selections restored
- ✅ Manual test: Multiple accounts maintain independent selections
- ✅ Manual test: Clear All removes selections from database
- ✅ No performance degradation (app still responsive)

---

## Estimated Test Count

- Step 5.1: 3 tests (model validation)
- Step 5.3: 15 tests (repository CRUD)
- Step 5.5: 10 tests (service persistence logic)
- Step 5.6: 8 tests (ViewModel auto-save/load)
- Step 5.8: 6 integration tests
- **Total**: ~42 new tests → **245 total tests** 🎯

---

## Ready to Start?

**Step 5.1** is ready to implement:

- Create `SyncConfiguration` model
- Simple record with 5 properties
- 3 basic tests

Let's get started! 🚀
