# OneDrive Sync Client - Architecture

## Overview

This document describes the architecture and core design decisions for the AStar OneDrive Sync Client.

## Layered Architecture

The application follows a clean layered architecture:

```text
┌──────────────────────────────────────────┐
│  Presentation Layer (AvaloniaUI)         │
│  - Views                                 │
│  - ViewModels (ReactiveUI)               │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│  Application Layer                       │
│  - Use Cases / Orchestration             │
│  - Command Handlers                      │
│  - Application Services                  │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│  Domain Layer                            │
│  - Domain Models                         │
│  - Domain Interfaces                     │
│  - Domain Services                       │
└──────────────────────────────────────────┘
                    ↓
┌──────────────────────────────────────────┐
│  Infrastructure Layer                    │
│  - EF Core & PostgreSQL                  │
│  - Secure Token Storage                  │
│  - Microsoft Graph API Client (Kiota V5) │
│  - File System Operations                │
│  - Telemetry & Logging                   │
└──────────────────────────────────────────┘
```

## Vertical-Slice Feature Structure

The codebase is organized by features rather than technical layers. Each feature contains all layers:

```text
src/AStar.Dev.OneDrive.Sync.Client/
├── Features/
│   ├── Authentication/
│   │   ├── Controllers/
│   │   ├── ViewModels/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── Repositories/
│   │   └── OAuth/
│   ├── AccountManagement/
│   ├── FileSync/
│   ├── ConflictResolution/
│   ├── Scheduling/
│   ├── Telemetry/
│   └── LogViewer/
├── Infrastructure/
│   ├── Database/
│   ├── SecureStorage/
│   ├── GraphApi/
│   └── Configuration/
├── Common/
│   ├── Models/
│   ├── Extensions/
│   ├── Constants/
│   ├── Exceptions/
│   └── Utilities/
└── Views/
```

See [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for detailed folder organization.

## Core Design Decisions

### Authentication & Secure Token Storage

#### OAuth 2.0 Flow

- Use MSAL (Microsoft Authentication Library) for token acquisition
- Device Code Flow or Interactive Browser Flow for cross-platform support
- Proactive token refresh: check expiry before each call; refresh if < 5 minutes
- Login timeout: 30 seconds with user-friendly notification

#### Cross-Platform Secure Storage

Platform-specific implementations via ISecureTokenStorage:

- **Windows**: DPAPI (DataProtectionScope.CurrentUser)
- **macOS**: Keychain
- **Linux**: SecretService D-Bus protocol
- **Fallback**: AES-256 encrypted file storage

#### Token Refresh Strategy

- Background task checks token expiry every 5 minutes
- Proactive refresh 5 minutes before expiry
- On-demand refresh with exponential backoff on failures

### Database Design

#### Schema Overview

PostgreSQL with EF Core using onedrive schema for isolation.

#### Key Tables

- **Accounts**: Core account data with hashed identifiers
- **DeltaTokens**: Per-account delta tokens for incremental sync
- **FileSystemItems**: File/folder tree with selection flags
- **ConflictLogs**: Sync conflict tracking and resolution
- **SyncHistory**: Audit trail of sync operations
- **DiagnosticSettings**: Per-account logging preferences
- **ApplicationLogs**: Structured logs for log viewer

See [database-schema.md](database-schema.md) for complete schema definition.

#### GDPR Compliance

- **Email Hashing**: `SHA256(email.ToLower())` for lookups
- **Account ID Hashing**: `SHA256(microsoftAccountId + createdAtTicks)`
- **No PII Storage**: No plaintext email or Microsoft account ID in database
- **Right to Erasure**: Cascade delete removes all associated data

### Feature Implementations

#### Delta Sync (Bidirectional)

1. Query active accounts
2. Retrieve delta tokens
3. Fetch remote changes via Graph API
4. Detect local changes via FileSystemWatcher
5. Compare hashes to identify conflicts
6. Queue downloads and uploads (concurrent, configurable limits)
7. Update delta tokens and sync history

#### Conflict Resolution

1. Detect conflicts during sync (both local and remote modified)
2. Log to ConflictLogs table
3. Prompt user with resolution options:
   - Keep Local
   - Keep Remote
   - Keep Both (rename local copy)
   - Ignore for Now
4. Apply resolution and update logs

#### Concurrent Operations

- SemaphoreSlim-based queues for downloads and uploads
- Configurable per account (default: 5 concurrent operations)
- Progress reporting and retry logic with exponential backoff

#### Background Scheduling

- Configurable sync interval (default: 5 minutes)
- Timer-based background service
- Checks for both remote and local changes
- Queues conflicts for user resolution

#### Telemetry & Logging

- Serilog with PostgreSQL sink (primary) and file fallback
- OpenTelemetry for traces and metrics
- Per-account diagnostic logging (configurable log level)
- Debug logging toggle with user notification
- Log retention: 15 days (30 days for critical errors)

#### Log Viewer

- Paged table view (100 rows per page by default)
- Account selector (including &quot;All Accounts&quot;)
- Filters: log level, message search
- Navigation: page forward/back

## Technology Stack

- **.NET 10**: Target framework
- **AvaloniaUI 11.3**: Cross-platform UI framework
- **ReactiveUI 22.3**: MVVM with reactive extensions
- **EF Core 10**: ORM for PostgreSQL
- **PostgreSQL 12+**: Database
- **MSAL**: Microsoft Authentication Library
- **Kiota V5**: Graph API client generation
- **Serilog**: Structured logging
- **OpenTelemetry**: Observability

See [dependencies.md](../README.md#dependencies) for complete package list.

## Security Considerations

- OAuth 2.0 with secure token storage per platform
- GDPR-compliant hashing for PII
- HTTPS-only Graph API communication
- Encrypted fallback storage (AES-256)
- Per-account debug logging with user warning
- No sensitive data in application logs

## Performance Considerations

- Concurrent download/upload queues (configurable limits)
- Delta sync for incremental changes only
- Database indexing for log viewer paging
- FileSystemWatcher debouncing for local changes
- Exponential backoff for API retries
- Multipart upload for large files (> 4MB)

---

**Document Version**: 1.0
**Last Updated**: February 3, 2026
**Related Documents**: [Implementation Roadmap](roadmap.md) | [Testing Strategy](testing-strategy.md) | [Configuration Reference](configuration.md)
