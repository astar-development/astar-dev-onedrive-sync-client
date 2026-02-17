# Sprint 4 Manual Testing Guide

## Overview

This guide covers manual testing for Sprint 4: Sync Tree UI with Tri-State Checkboxes.

**Date**: January 6, 2026
**Sprint**: 4 - Steps 4.1-4.8
**Status**: Ready for Manual Testing

---

## Prerequisites

Before testing, ensure:

1. ✅ All 202 unit tests passing (5 integration tests properly skipped)
2. ✅ Application builds successfully
3. ✅ You have a valid Microsoft account with OneDrive access
4. ✅ ClientId configured in `appsettings.json`

---

## Test Scenarios

### Scenario 1: Account Selection and Folder Loading

**Purpose**: Verify that selecting an account automatically loads its OneDrive folder tree.

**Steps**:

1. Launch the application: `dotnet run --project src/AStarOneDriveClient`
2. Click "Add Account" in the left panel
3. Complete the OAuth login flow in the browser
4. Observe the account appears in the left panel
5. Click the newly added account to select it

**Expected Results**:

- ✅ Right panel shows "Loading folders..." indicator
- ✅ After 1-3 seconds, folder tree appears
- ✅ Root-level folders display with folder icons
- ✅ Each folder shows its name and OneDrive path
- ✅ Checkboxes are all unchecked (empty) by default

**Fail Conditions**:

- ❌ Folders don't load after selection
- ❌ Error message appears
- ❌ Checkboxes missing or incorrectly displayed

---

### Scenario 2: Tri-State Checkbox Behavior - Cascading Down

**Purpose**: Verify that checking a parent folder checks all its children.

**Steps**:

1. Ensure an account is selected with folders loaded
2. Expand a folder that has children (click the arrow/expander)
3. Check the parent folder's checkbox (click to select)
4. Observe all child folders

**Expected Results**:

- ✅ Parent checkbox shows checkmark (✓)
- ✅ ALL child folders automatically become checked
- ✅ If children have children, those are also checked (recursive)

**Fail Conditions**:

- ❌ Some children remain unchecked
- ❌ Checkboxes don't update visually
- ❌ Application crashes or hangs

---

### Scenario 3: Tri-State Checkbox Behavior - Propagating Up (Indeterminate)

**Purpose**: Verify that partially selecting children sets parent to indeterminate state.

**Steps**:

1. Expand a folder with multiple children (at least 3)
2. Check ONLY ONE child folder
3. Observe the parent folder's checkbox

**Expected Results**:

- ✅ Parent checkbox shows **indeterminate** state (filled square ■ or dash -)
- ✅ Not fully checked (✓) or unchecked (empty)

**Steps (continue)**:
4. Check ALL remaining children
5. Observe parent checkbox again

**Expected Results**:

- ✅ Parent checkbox changes from indeterminate to **fully checked** (✓)

**Fail Conditions**:

- ❌ Parent shows checked when only some children are checked
- ❌ Parent doesn't update when children change
- ❌ Indeterminate state looks like checked or unchecked

---

### Scenario 4: Tri-State Checkbox Behavior - Propagating Up (Checked)

**Purpose**: Verify that checking all children checks the parent.

**Steps**:

1. Expand a folder with 2-3 children
2. Manually check each child one by one
3. Observe parent checkbox after each click

**Expected Results**:

- ✅ After first child: parent becomes **indeterminate** (■)
- ✅ After second child (if not all): parent remains **indeterminate**
- ✅ After ALL children checked: parent becomes **checked** (✓)

**Fail Conditions**:

- ❌ Parent doesn't become indeterminate
- ❌ Parent becomes checked before all children are checked
- ❌ Checking last child doesn't update parent

---

### Scenario 5: Unchecking Behavior

**Purpose**: Verify that unchecking a parent unchecks all children.

**Steps**:

1. Check a parent folder (all children should be checked)
2. Click the parent checkbox again to uncheck it
3. Observe all children

**Expected Results**:

- ✅ Parent checkbox becomes unchecked (empty)
- ✅ ALL child checkboxes become unchecked
- ✅ Recursive: grandchildren also unchecked

**Fail Conditions**:

- ❌ Some children remain checked
- ❌ Only direct children unchecked (grandchildren still checked)

---

### Scenario 6: Indeterminate to Checked Transition

**Purpose**: Verify clicking indeterminate checkbox checks it and all children.

**Steps**:

1. Get a parent into **indeterminate** state (check only some children)
2. Click the parent checkbox (currently indeterminate)
3. Observe parent and children

**Expected Results**:

- ✅ Parent changes from indeterminate to **checked** (✓)
- ✅ ALL children become checked (even previously unchecked ones)

**Fail Conditions**:

- ❌ Parent goes to unchecked instead of checked
- ❌ Children don't all become checked

---

### Scenario 7: Clear All Selections

**Purpose**: Verify "Clear All" button resets all checkboxes.

**Steps**:

1. Check several folders at various levels (root and nested)
2. Verify some checkboxes are checked and/or indeterminate
3. Click "Clear All" button in the right panel header
4. Observe all checkboxes

**Expected Results**:

- ✅ ALL checkboxes become unchecked (empty)
- ✅ No checked or indeterminate states remain
- ✅ Button action is instant (no delay)

**Fail Conditions**:

- ❌ Some checkboxes remain checked/indeterminate
- ❌ Button doesn't work
- ❌ Application crashes

---

### Scenario 8: Account Switching

**Purpose**: Verify switching accounts loads different folder trees.

**Prerequisites**: Add 2 different accounts first.

**Steps**:

1. Select Account A from the left panel
2. Wait for folders to load
3. Note the folder names displayed
4. Select Account B from the left panel
5. Wait for folders to load
6. Compare folder names

**Expected Results**:

- ✅ Account A shows its OneDrive folders
- ✅ Account B shows its OneDrive folders (different content)
- ✅ Switching is smooth with loading indicator
- ✅ Previous selections don't affect new account

**Fail Conditions**:

- ❌ Both accounts show the same folders
- ❌ Folders don't change when switching
- ❌ Previous selections carry over

---

### Scenario 9: Deselecting Account

**Purpose**: Verify deselecting an account clears the folder tree.

**Steps**:

1. Select an account with folders loaded
2. Click the selected account again (or click empty space to deselect)
3. Observe the right panel

**Expected Results**:

- ✅ Right panel clears (no folders shown)
- ✅ Either empty state or placeholder text appears
- ✅ No checkboxes remain visible

**Fail Conditions**:

- ❌ Folders remain displayed
- ❌ Cannot deselect account

---

### Scenario 10: Error Handling - Unauthenticated Account

**Purpose**: Verify graceful error when selecting unauthenticated account.

**Steps**:

1. Add an account and log in
2. Log out the account (click Logout button)
3. Try to select the now-unauthenticated account
4. Observe the right panel

**Expected Results**:

- ✅ Error message displays (e.g., "Account not authenticated")
- ✅ No folders load
- ✅ Error message is user-friendly
- ✅ Application remains responsive

**Fail Conditions**:

- ❌ Application crashes
- ❌ No error message shown
- ❌ Confusing technical error displayed

---

### Scenario 11: Lazy Loading Children

**Purpose**: Verify child folders load only when parent is expanded.

**Steps**:

1. Select an account with loaded root folders
2. Observe a folder with children (has expander arrow ▶)
3. Note the folder is collapsed (children not visible)
4. Click the expander arrow to expand the folder
5. Observe loading behavior

**Expected Results**:

- ✅ Clicking expander shows loading indicator (if async)
- ✅ Child folders appear after expansion
- ✅ Children were not loaded initially (efficient)

**Note**: Current implementation may load all children upfront. This test verifies current behavior.

**Fail Conditions**:

- ❌ Expanding folder causes error
- ❌ Children don't appear
- ❌ Application freezes during expansion

---

### Scenario 12: Visual Consistency

**Purpose**: Verify UI elements are properly styled and aligned.

**Steps**:

1. Launch application and load folders
2. Inspect visual elements:
   - Folder icons
   - Checkbox alignment
   - Text alignment
   - Indentation levels
   - Colors and contrast

**Expected Results**:

- ✅ Folder icons (📁) display correctly
- ✅ Checkboxes aligned vertically
- ✅ Folder names are readable (good contrast)
- ✅ Nested folders properly indented
- ✅ Path text (subtle, smaller) visible but not prominent
- ✅ Loading indicator is clear
- ✅ Error messages use warning color (red/orange)

**Fail Conditions**:

- ❌ Icons missing or broken
- ❌ Misaligned elements
- ❌ Text unreadable (poor contrast)
- ❌ No visual hierarchy

---

## Test Results Template

Copy and fill this out after testing:

``` text
=== SPRINT 4 MANUAL TESTING RESULTS ===

Date: _______________
Tester: _______________

Scenario 1 (Account Selection): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 2 (Cascading Down): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 3 (Indeterminate State): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 4 (Propagating Up): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 5 (Unchecking): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 6 (Indeterminate → Checked): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 7 (Clear All): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 8 (Account Switching): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 9 (Deselecting Account): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 10 (Error Handling): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 11 (Lazy Loading): [ ] PASS  [ ] FAIL
Notes: ____________________________________

Scenario 12 (Visual Consistency): [ ] PASS  [ ] FAIL
Notes: ____________________________________

=== OVERALL STATUS ===
[ ] All scenarios PASSED - ready for next sprint
[ ] Some failures - requires fixes
[ ] Major issues - requires rework

Critical Issues Found:
_______________________________________
_______________________________________
```

---

## Running the Application

### Quick Start

```bash
cd c:\repos\astar-development\astar-dev-onedrive-client-v3
dotnet run --project src/AStarOneDriveClient
```

### With Detailed Logging

```bash
$env:DOTNET_LOGGING__CONSOLE__LOGLEVEL__DEFAULT="Debug"
dotnet run --project src/AStarOneDriveClient
```

---

## Known Limitations (Expected Behavior)

1. **Selections not persisted**: Closing and reopening the app resets all selections (Sprint 5 will add persistence)
2. **No conflict resolution**: Not implemented yet (Sprint 7)
3. **No actual sync**: Just selection UI, sync engine comes in Sprint 6
4. **Authentication state**: May require re-authentication after token expiry

---

## Troubleshooting

### Issue: Folders won't load

- **Check**: Is the account authenticated? (green indicator)
- **Check**: Internet connection working?
- **Check**: Check console output for errors

### Issue: Checkboxes don't respond

- **Check**: Are you clicking the checkbox itself (not just text)?
- **Check**: Is the folder tree still loading?

### Issue: "Account not authenticated" error

- **Solution**: Click "Login" button for the account
- **Solution**: Complete OAuth flow in browser

### Issue: Application won't start

- **Check**: Run `dotnet build` first
- **Check**: Ensure .NET 10 SDK installed
- **Check**: Database file permissions (%LocalAppData%\AStarOneDriveClient\)

---

## Next Steps After Testing

1. Document any bugs found in GitHub Issues
2. Update this guide with actual findings
3. If all tests pass, mark Sprint 4 complete
4. Proceed to Sprint 5: Database Persistence for Selections

**Sprint 4 Complete ✅** when:

- All 12 scenarios pass
- No critical bugs
- Visual polish acceptable
- Ready for database integration (Sprint 5)
