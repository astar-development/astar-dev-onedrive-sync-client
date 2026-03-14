---
# Potential Refactoring Targets: Core Layer

This document highlights the top 10 methods in the `AStar.Dev.OneDrive.Sync.Client.Core` project that require refactoring, based on code complexity, maintainability, and code smell indicators. Review and prioritize these for technical debt reduction.

## 1. `SyncState.CreateInitial`, `Create`, `CreateFailed`
**File:** `Models/SyncState.cs`  
**Issues:** Static factory methods with many parameters, risk of parameter order bugs, hard to extend.  
**Suggestion:** Use builder pattern or named parameters, document parameter order.

## 2. `DebugLogMetadata` nested static classes
**File:** `DebugLogMetadata.cs`  
**Issues:** Deeply nested static classes/constants, hard to discover and maintain.  
**Suggestion:** Flatten structure, consider using enums or a config file for log keys.

## 3. `AccountInfoExtensions` methods
**File:** `Models/AccountInfoExtensions.cs`  
**Issues:** Extension methods may hide business logic, risk of misuse.  
**Suggestion:** Move critical logic to domain models, limit extension method scope.

## 4. `OneDriveResponse`/`Value`/`ParentReference`/`Hashes`/`View` direct mappings
**File:** `Models/OneDrive/*.cs`  
**Issues:** Direct API mappings with little validation, risk of nulls and inconsistent state.  
**Suggestion:** Add validation, consider value objects for critical fields.

## 5. `SyncDirection` and `SyncStatus` enums
**File:** `Models/Enums/SyncDirection.cs`, `SyncStatus.cs`  
**Issues:** Enums may not cover all future states, risk of magic numbers.  
**Suggestion:** Use discriminated unions or add extension methods for safe handling.

## 6. `ApplicationMetadata` constants
**File:** `ApplicationMetadata.cs`  
**Issues:** Static constants scattered, risk of duplication, hard to update.  
**Suggestion:** Centralize metadata, consider config-driven approach.

## 7. `SyncState` constructor
**File:** `Models/SyncState.cs`  
**Issues:** Many parameters, hard to maintain, risk of bugs.  
**Suggestion:** Use builder or factory with named arguments.

## 8. `Value` class property mapping
**File:** `Models/OneDrive/Value.cs`  
**Issues:** Many nullable properties, risk of null reference bugs.  
**Suggestion:** Add null checks, use Option types where possible.

## 9. `ParentReference`/`FileSystemInfo`/`Image` direct usage
**File:** `Models/OneDrive/*.cs`  
**Issues:** No encapsulation, direct property access, risk of inconsistent state.  
**Suggestion:** Encapsulate with methods or value objects.

## 10. Lack of domain validation in model constructors
**File:** `Models/OneDrive/*.cs`, `Models/SyncState.cs`  
**Issues:** Constructors allow invalid state, no guard clauses.  
**Suggestion:** Add validation logic, use static factory methods for safe creation.

---
**Note:** This list is based on static analysis and code structure. Actual refactoring priority should consider test coverage, bug history, and business impact.