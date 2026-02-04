# Implementation Roadmap

This document outlines the complete implementation plan for the OneDrive Sync Client, organized into 9 phases.

> **IMPORTANT**: Each checkbox represents a single, independent task that should be implemented and tested separately. Do NOT combine multiple tasks. Each task should result in a focused, reviewable pull request.

## Phase 0: Foundation Setup

**Purpose**: Set up project settings and initial configuration.

### Task 0.1: Configuration Files ✅

- [x] Define connection strings, logging settings, OAuth client IDs
- [x] Configure environment-specific overrides
- [x] Validate configuration loading in Program.cs
- [x] Add User Secrets for local development
- [x] Document configuration options in README.md
- [x] Verify configuration binding with unit tests
- [x] Create Entra ID app registration guide

### Task 0.2: Polly Retry Policies ✅

- [x] Define standard retry policies for transient failures
- [x] Create documentation with examples
- [x] Include guidelines for exponential backoff and circuit breaker patterns
- [x] Review and validate policies with team

---

### Phase 1: Foundation (Layers & DI)

#### Purpose

Establish the foundational architecture, dependency injection, and database infrastructure.

**Task 1.1**: Set up project structure ✅

- [x] Create `Features/` folder structure for all feature slices
- [x] Create `Common/` folder for shared models, extensions, constants
- [x] Create `Infrastructure/` folder for cross-cutting concerns

**Task 1.2**: Configure Dependency Injection ✅

- [x] Add `Microsoft.Extensions.DependencyInjection` NuGet package
- [x] Create `AppModule.cs` for DI container registration
- [x] Configure service lifetimes (Singleton, Scoped, Transient)

**Task 1.3**: Add core NuGet packages ✅

- [x] Add EF Core and PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- [x] Add MSAL for OAuth authentication
- [x] Add Kiota abstractions for Graph API

**Task 1.4**: Add UI NuGet packages ✅

- [x] Add AvaloniaUI core package
- [x] Add Avalonia.ReactiveUI for MVVM integration
- [x] Add ReactiveUI framework

**Task 1.5**: Add observability NuGet packages ✅

- [x] Add Serilog and PostgreSQL sink

**Task 1.6**: Create DbContext with schema configuration ✅

- [x] Create `OneDriveSyncDbContext` class
- [x] Configure `onedrive` schema in `OnModelCreating`
- [x] Add connection string to `appsettings.json`

**Task 1.7**: Create initial database migrations ✅

- [x] Add migration for `Accounts` table with hashing fields
- [x] Add migration for `DeltaTokens` table
- [x] Add migration for `FileSystemItems` table with hash tracking

> **Task 1.8**: Create remaining database migrations ✅

- [x] Add migration for `ConflictLogs` table
- [x] Add migration for `SyncHistory` table
- [x] Add migration for `DiagnosticSettings` table

> **Task 1.9**: Create logging table migration ✅

- [x] Add migration for `ApplicationLogs` table with indexes
- [x] Verify all foreign key constraints are correctly configured

> **Task 1.10**: Implement `ISecureTokenStorage` abstraction and factory ✅

- [x] Define `ISecureTokenStorage` interface in Infrastructure layer
- [x] Create `SecureTokenStorageFactory` for platform detection
- [x] Implement `WindowsSecureTokenStorage` (DPAPI-based)
- [x] Implement `MacOSSecureTokenStorage` (Keychain-based)
- [x] Implement `LinuxSecureTokenStorage` (SecretService D-Bus)
- [x] Implement `AesSecureTokenStorage` (AES-256 encrypted fallback)
- [x] Create factory pattern for platform-specific selection

- [ ] Create base test class with common scenarios (SecureTokenStorageTestsBase)
- [ ] Add unit tests for Windows DPAPI storage with encryption/decryption verification
- [ ] Add unit tests for AES-256 encrypted storage with integrity checks
- [ ] Add unit tests for SecureTokenStorageFactory platform detection
- [ ] Verify all 71 tests pass with Xunit + Shouldly
- [ ] Validate tamper detection and error handling

---

See [plan-onedriveSyncClient.prompt.md](plan-onedriveSyncClient.prompt.md) for complete task details for Phases 1-8.

---

**Document Version**: 1.0
**Last Updated**: February 3, 2026
**Related Documents**: [Architecture](architecture.md) | [Testing Strategy](testing-strategy.md)
