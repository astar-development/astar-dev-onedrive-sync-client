# Quick Start Guide

Get up and running with the AStar Dev OneDrive Sync Client development environment.

---

## Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2022 / VS Code / Rider
- Git
- SQLite (included with .NET)

---

## Build Commands

### Build Solution
```bash
dotnet build
```

### Clean Build
```bash
dotnet clean
dotnet build
```

### Build Specific Project
```bash
dotnet build src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/
```

---

## Run / Debug

### Run Application
```bash
dotnet run --project src/AStar.Dev.OneDrive.Sync.Client/
```

### Watch Mode (Auto-Reload on Changes)
```bash
dotnet watch run --project src/AStar.Dev.OneDrive.Sync.Client/
```

### Debug in Visual Studio
- Open `.slnx` solution file
- Set `AStar.Dev.OneDrive.Sync.Client` as startup project
- Press F5 to start debugging

### Debug in VS Code
- Open workspace folder
- Use launch configuration: "Debug AStar OneDrive Sync Client"
- Press F5 to start debugging

---

## Testing

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test test/AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/
dotnet test test/AStar.Dev.OneDrive.Sync.Client.Tests.Integration/
```

### Run Specific Test Class
```bash
dotnet test --filter WindowPreferencesServiceShould
```

### Run Specific Test Method
```bash
dotnet test --filter "FullyQualifiedName~WindowPreferencesServiceShould.ReturnNullWhenNoPreferencesExist"
```

### Watch Mode (Auto-Rerun on Changes)
```bash
dotnet watch test
```

### With Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Entity Framework Migrations

### Create Migration
```bash
dotnet ef migrations add <MigrationName> \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client
```

**Example**:
```bash
dotnet ef migrations add AddSyncConflictTable \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client
```

### Apply Migrations
```bash
dotnet ef database update \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client
```

### Apply Specific Migration
```bash
dotnet ef database update <MigrationName> \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client
```

### List Migrations
```bash
dotnet ef migrations list \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client
```

### Remove Last Migration (Before Applying)
```bash
dotnet ef migrations remove \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client
```

### Generate SQL Script
```bash
dotnet ef migrations script \
  --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Sync.Client \
  --output migration.sql
```

---

## Package Management

### Restore NuGet Packages
```bash
dotnet restore
```

### Add Package to Project
```bash
dotnet add src/AStar.Dev.OneDrive.Sync.Client/ package <PackageName>
```

### Update Package
```bash
dotnet add src/AStar.Dev.OneDrive.Sync.Client/ package <PackageName> --version <Version>
```

### List Outdated Packages
```bash
dotnet list package --outdated
```

---

## Database Management

### Database Location
- **Windows**: `%APPDATA%/astar-dev-onedrive-sync-client/sync.db`
- **Linux**: `~/.local/share/astar-dev-onedrive-sync-client/sync.db`
- **macOS**: `~/Library/Application Support/astar-dev-onedrive-sync-client/sync.db`

### View Database (SQLite CLI)
```bash
sqlite3 "/path/to/sync.db"
```

### Common Queries
```sql
-- View accounts
SELECT * FROM Accounts;

-- View sync items
SELECT * FROM SyncItems WHERE AccountId = 'account-id';

-- View conflicts
SELECT * FROM SyncConflicts WHERE IsResolved = 0;

-- View debug logs
SELECT * FROM DebugLogs ORDER BY Timestamp DESC LIMIT 100;
```

### Reset Database
```bash
# Delete database file
rm "/path/to/sync.db"

# Restart application (will recreate with migrations)
dotnet run --project src/AStar.Dev.OneDrive.Sync.Client/
```

---

## Development Workflow

### 1. Create Feature Branch
```bash
git checkout -b feature/your-feature-name
```

### 2. Write Failing Test (TDD Red Phase)
```bash
# Create test file
# Write test that fails
dotnet test

# Commit failing test
git add .
git commit -m "test: add test for new feature (failing)"
```

### 3. Implement Feature (TDD Green Phase)
```bash
# Implement minimum code to pass test
dotnet test

# Commit passing implementation
git add .
git commit -m "feat: implement new feature"
```

### 4. Refactor (TDD Refactor Phase)
```bash
# Improve code while keeping tests green
dotnet test

# Commit refactoring
git add .
git commit -m "refactor: improve feature implementation"
```

### 5. Push and Create PR
```bash
git push -u origin feature/your-feature-name
# Create PR via GitHub UI or API
```

---

## Useful Commands

### Check Code Style
```bash
dotnet format --verify-no-changes
```

### Fix Code Style
```bash
dotnet format
```

### Generate Documentation
```bash
dotnet tool install -g docfx
docfx docs/docfx.json --serve
```

### Clean All Build Artifacts
```bash
git clean -xfd
dotnet restore
dotnet build
```

---

## Next Steps

- Read [Architecture Overview](../README.md)
- Review [Development Tasks](../.github/instructions/development-tasks.instructions.md)
- Check [Style Guidelines](../.github/instructions/style-guidelines.instructions.md)
- Explore [Implementation Patterns](../.github/instructions/implementation-patterns.instructions.md)
