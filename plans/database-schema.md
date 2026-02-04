# Database Schema

This document defines the complete PostgreSQL database schema for the OneDrive Sync Client.

## Schema Overview

All tables reside in the `onedrive` schema for isolation from other applications.

## Requirements

- PostgreSQL 12 or higher
- Connection string configured in `appsettings.json`
- EF Core migrations applied on application startup

## Tables

```sql
-- Core account table
CREATE TABLE onedrive.Accounts (
    Id TEXT PRIMARY KEY,                    -- Hashed AccountId (SHA256 of email + timestamp)
    HashedEmail TEXT NOT NULL UNIQUE,       -- Hashed email for lookups
    DisplayName TEXT,
    TokenStorageKey TEXT,                   -- Reference to secure storage
    HomeSyncDirectory TEXT,                 -- User-configurable local sync path
    MaxConcurrentDownloads INT DEFAULT 5,   -- Concurrent download limit
    MaxConcurrentUploads INT DEFAULT 5,     -- Concurrent upload limit
    EnableDebugLogging BOOLEAN DEFAULT FALSE, -- Per-account debug logging
    CreatedAt TIMESTAMP,
    LastAuthRefresh TIMESTAMP,
    IsActive BOOLEAN
);

-- Delta token per account (for incremental sync)
CREATE TABLE onedrive.DeltaTokens (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    DriveName TEXT NOT NULL,                -- e.g., "root", "documents", "photos"
    Token TEXT,                             -- Opaque delta token from Graph API
    LastSyncAt TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- File/folder tree with selection flag
CREATE TABLE onedrive.FileSystemItems (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    DriveItemId TEXT NOT NULL,              -- OneDrive item ID
    Name TEXT,
    Path TEXT,
    IsFolder BOOLEAN,
    ParentItemId TEXT,                      -- NULL for root
    IsSelected BOOLEAN DEFAULT FALSE,       -- User selection flag
    LocalPath TEXT,                         -- Synced to this local path
    RemoteModifiedAt TIMESTAMP,
    LocalModifiedAt TIMESTAMP,
    RemoteHash TEXT,                        -- Remote file hash (for change detection)
    LocalHash TEXT,                         -- Local file hash (for change detection)
    SyncStatus TEXT,                        -- 'synced', 'pending_upload', 'pending_download', 'conflict', 'failed'
    LastSyncDirection TEXT,                 -- 'upload', 'download', 'bidirectional'
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Conflict log for resolution
CREATE TABLE onedrive.ConflictLogs (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    ItemId TEXT NOT NULL,
    LocalPath TEXT,
    ConflictType TEXT,                      -- 'local_newer', 'remote_newer', 'both_modified'
    LocalLastModified TIMESTAMP,
    RemoteLastModified TIMESTAMP,
    ResolutionAction TEXT,                  -- 'keep_local', 'keep_remote', 'both', 'ignore'
    ResolvedAt TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Sync history for auditing
CREATE TABLE onedrive.SyncHistory (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL,
    SyncType TEXT,                          -- 'manual', 'scheduled', 'background'
    SyncDirection TEXT,                     -- 'upload', 'download', 'bidirectional'
    StartedAt TIMESTAMP,
    CompletedAt TIMESTAMP,
    Status TEXT,                            -- 'success', 'partial', 'failed'
    ItemsUploaded INT,
    ItemsDownloaded INT,
    ErrorMessage TEXT,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- User diagnostic settings
CREATE TABLE onedrive.DiagnosticSettings (
    Id TEXT PRIMARY KEY,
    AccountId TEXT NOT NULL UNIQUE,
    LogLevel TEXT,                          -- 'Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'
    IsEnabled BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Application logs (for log viewer)
CREATE TABLE onedrive.ApplicationLogs (
    Id BIGSERIAL PRIMARY KEY,
    AccountId TEXT,                         -- NULL for global logs
    LogLevel TEXT NOT NULL,
    Timestamp TIMESTAMP NOT NULL DEFAULT NOW(),
    Message TEXT,
    Exception TEXT,
    SourceContext TEXT,                     -- Logger name/category
    Properties JSONB,                       -- Structured log properties
    FOREIGN KEY (AccountId) REFERENCES onedrive.Accounts(Id)
);

-- Index for log viewer paging
CREATE INDEX idx_applicationlogs_accountid_timestamp ON onedrive.ApplicationLogs(AccountId, Timestamp DESC);
CREATE INDEX idx_applicationlogs_loglevel ON onedrive.ApplicationLogs(LogLevel);
```

## Indexes

```sql
CREATE INDEX idx_applicationlogs_accountid_timestamp 
  ON onedrive.ApplicationLogs(AccountId, Timestamp DESC);
  
CREATE INDEX idx_applicationlogs_loglevel 
  ON onedrive.ApplicationLogs(LogLevel);
```

## Migration Strategy

- Migrations applied automatically on startup if pending
- No data loss during migrations (ALTER TABLE statements only)
- Schema creation handled via EF Core migrations
- Foreign key constraints enforced for referential integrity

## GDPR Compliance

### Hashing Strategy

- **Email Hashing**: `SHA256(email.ToLower())` for unique account lookup
- **Account ID Hashing**: `SHA256(microsoftAccountId + createdAtTicks)`
- **No PII Storage**: No plaintext email or Microsoft account ID in database
- **Display Names**: User-provided display name or custom nickname
- **Secure Storage Mapping**: Encrypted email references in platform-specific secure storage

### Right to Erasure

Deleting an account record:

1. Cascade delete all related data (DeltaTokens, FileSystemItems, ConflictLogs, SyncHistory, DiagnosticSettings, ApplicationLogs)
2. Clear secure storage separately via platform APIs
3. No orphaned data remains after deletion

## Maintenance

### Log Retention

- Standard logs: Purge after 15 days
- Critical errors: Retain for 30 days
- Manual clearing available via UI
- Scheduled job runs daily to enforce retention policy

---

**Document Version**: 1.0
**Last Updated**: February 3, 2026
**Related Documents**: [Architecture](architecture.md) | [Configuration Reference](configuration.md)
