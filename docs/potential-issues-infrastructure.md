---
# Potential Refactoring Targets: Infrastructure Layer

This document highlights the top 10 methods in the `AStar.Dev.OneDrive.Sync.Client.Infrastructure` project that require refactoring, based on code complexity, maintainability, and code smell indicators. Review and prioritize these for technical debt reduction.

## 1. `SyncEngine.StartSyncAsync`
**File:** `Services/SyncEngine.cs`  
**Issues:** Very large method, mixes orchestration, error handling, and state updates. Difficult to test and maintain.  
**Suggestion:** Extract phases (validation, download, upload, state update) into private helpers or strategy classes.

## 2. `SyncEngine.ValidateAndGetAccountAsync`
**File:** `Services/SyncEngine.cs`  
**Issues:** Uses exceptions for control flow, mixes Result/Option with exceptions.  
**Suggestion:** Refactor to use only Result/Option for error handling, avoid exceptions for expected cases.

## 3. `SyncEngine.CalculateSyncSummaryAsync` and related summary methods
**File:** `Services/SyncEngine.cs`  
**Issues:** Large, complex logic for calculating sync stats, repeated grouping/filtering.  
**Suggestion:** Decompose into smaller, testable units; consider moving to a dedicated summary service.

## 4. `SyncEngine.DetectFilesToUploadAsync` and `DetectFilesToDownloadAndConflictsAsync`
**File:** `Services/SyncEngine.cs`  
**Issues:** Large static methods, complex conditional logic, hard to test.  
**Suggestion:** Split into smaller methods, extract filtering/matching logic.

## 5. `SyncSelectionService.NormalizePathForComparison`
**File:** `Services/SyncSelectionService.cs`  
**Issues:** Complex regex and string manipulation, risk of subtle bugs.  
**Suggestion:** Add unit tests, clarify intent, consider using a utility class for path normalization.

## 6. `SyncConfigurationRepository.CleanUpPath` and `Normalize`
**File:** `Repositories/SyncConfigurationRepository.cs`  
**Issues:** String manipulation with unclear edge case handling.  
**Suggestion:** Add tests for edge cases, document intent, consider using Path APIs.

## 7. `GraphApiClient.GetDriveRootAsync` and related Graph API wrappers
**File:** `Services/GraphApiClient.cs`  
**Issues:** Repeated client creation, error handling not always robust.  
**Suggestion:** Centralize Graph client creation, improve error handling, add logging.

## 8. `DebugLoggerService.LogAsync`
**File:** `Services/DebugLoggerService.cs`  
**Issues:** Handles multiple concerns (account lookup, string truncation, DB write) in one method.  
**Suggestion:** Split responsibilities, add tests for truncation logic.

## 9. `DebugLogRepository.GetByAccountIdAsync` (overloads)
**File:** `Repositories/DebugLogRepository.cs`  
**Issues:** Multiple overloads, possible code duplication, paging logic could be unified.  
**Suggestion:** Refactor to a single method with optional parameters, clarify paging contract.

## 10. `ConflictDetectionService.CheckIfLocalFileHasChanged`
**File:** `Services/ConflictDetectionService.cs`  
**Issues:** Magic numbers (e.g., time threshold), unclear intent for change detection.  
**Suggestion:** Replace magic numbers with named constants, document logic, add tests for edge cases.

---
**Note:** This list is based on static analysis and code structure. Actual refactoring priority should consider test coverage, bug history, and business impact.