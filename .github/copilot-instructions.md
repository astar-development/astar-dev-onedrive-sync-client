# Copilot Instructions - AStar Dev OneDrive Sync Client

## Project Overview

**AStar Dev OneDrive Sync Client** is a cross-platform desktop application that provides bidirectional synchronization between local file systems and Microsoft OneDrive. Built with Avalonia (cross-platform UI framework) and .NET 10, it features intelligent conflict detection, multi-account support, and efficient delta-based synchronization.

### Key Characteristics

- **Language**: C# 14
- **Target Framework**: .NET 10.0
- **UI Framework**: Avalonia 11.3.11 with ReactiveUI
- **Database**: SQLite with Entity Framework Core 10.0.2
- **Authentication**: MSAL (Microsoft.Identity.Client 4.81.0)
- **API Integration**: Microsoft Graph API 5.101.0
- **Async Patterns**: System.Reactive 6.1.0
- **Testing**: xUnit with in-memory databases and mocking

---

## Architecture Overview

### Layered Architecture

- **Components**:
  **TDD Workflow (Enforced)**

- **Write failing tests first**: For any new behavior, create one or more tests that fail before adding production code. Tests should specify expected behavior and be the driver for the minimal implementation.
- **Local verification**: Developers must run the test suite locally and confirm the new test fails, then implement production code to make it pass. Run the test suite again and confirm no other tests regressed.
- **Commit practice**: Include the failing-test commit in feature branches (failing-test commit either first or clearly present in PR history) so reviewers can see the TDD progression.

**Test Organization**:

- **Unit Tests**: `test/AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit/`
- **Core Tests**: `test/AStar.Dev.OneDrive.Client.Core.Tests.Unit/`
- **Integration Tests**: `test/AStar.Dev.OneDrive.Client.Tests.Integration/`
- **UI Tests**: `test/AStar.Dev.OneDrive.Client.Tests.Unit/`
- **NuGet Package Tests**: `test/nuget-packages/`

**Testing Patterns & TDD Practices**:

1. **Naming Convention**: `<ComponentName>Should`

   ```csharp
   public class WindowPreferencesServiceShould { }
   ```

2. **Fact vs Theory**:
   - `[Fact]` - Single test case
   - `[Theory] [InlineData(...)]` - Parameterized tests

3. **In-Memory Database Testing**:

   ```csharp
   using SyncDbContext context = CreateInMemoryContext();
   var service = new WindowPreferencesService(context);
   ```

4. **Mocking Pattern**: Create mocks via interface, use with services. Prefer behavior-driven assertions and avoid coupling tests to private implementation details.

5. **Assertion Library**: Shouldly (for English-language, fluent-style assertions)

   ```csharp
   result.ShouldNotBeNull();
   result.ShouldBe(expectedValue);
   ```

6. **CI Enforcement**: CI must run the full test suite and fail the build if any test fails. PRs must pass CI before merge. See CI guidance below.

- `AutoSyncSchedulerService` - Scheduled sync execution
- `AuthenticationClient` - OAuth/MSAL wrapper
- `DebugLoggerService` - Application-wide logging
- `WindowPreferencesService` - UI state persistence

**Key Repositories** (using Repository pattern):

- `IAccountRepository` - Account metadata and authentication tokens
- `ISyncRepository` - Sync state and session tracking
- `IDriveItemsRepository` - Remote file metadata caching
- `ISyncConfigurationRepository` - User sync folder selections
- `ISyncConflictRepository` - Conflict records and resolutions
- `IFileOperationLogRepository` - Historical file operations
- `IDebugLogRepository` - Application logs

#### 3. **Core/Domain Layer** (`AStar.Dev.OneDrive.Client.Core`)

- **Models**: Domain objects and data structures
- **Data Entities**: EF Core entity definitions
- **Enums**: Sync state and configuration enums

**Key Models**:

- `AccountInfo` - User account and authentication state
- `FileMetadata` - File properties and sync status
- `SyncConfiguration` - User-defined sync selections
- `SyncState` - Current synchronization status
- `SyncConflict` - Conflict information and resolution
- `SyncSessionLog` - Sync operation history
- `WindowPreferences` - UI preferences persistence

### Dependency Injection

The solution uses **Microsoft.Extensions.DependencyInjection** with custom source generators:

- **Service Decorator**: `[Service]` attribute (custom)
- **Lifetime Options**: Scoped (default), Singleton, Transient
- **Generator**: `AStar.Dev.Source.Generators` auto-generates `ServiceCollectionExtensions`
- **Configuration**: Services are automatically registered based on attributes
- **Access**: Services accessed via `App.Host.Services` (ServiceLocator pattern)

```csharp
[Service(ServiceLifetime.Scoped, As = typeof(IMyInterface))]
public class MyImplementation : IMyInterface { }
```

### Supporting NuGet Packages

Custom internal packages (located in `src/nuget-packages/`):

- **AStar.Dev.Functional.Extensions** - Result<T>, Option<T>, functional programming utilities
- **AStar.Dev.Logging.Extensions** - Structured logging helpers
- **AStar.Dev.Source.Generators** - Service registration and options binding code generation
- **AStar.Dev.Utilities** - Common utilities (JSON, regex, string extensions)

---

## Development Approach & Patterns

**Test-Driven Development (TDD) Policy**

- **Mandate**: All new features, bug fixes, and refactors MUST follow Test-Driven Development (TDD). Developers must write one or more failing tests that express the desired behavior before writing production code. Only after seeing the tests fail should production code be implemented to make the tests pass, followed by a refactor step while keeping tests green.
- **Failing-First Workflow**: The minimal TDD loop is: write a failing test -> run tests to verify failure -> implement minimal production code -> run tests until they pass -> refactor with tests green.
- **Test Granularity**: Prefer small, focused unit tests that assert behavior rather than internal implementation. Use integration tests for cross-service flows and E2E tests sparingly for user-facing scenarios.
- **Mocks & Test Doubles**: Use interface-based abstractions (existing repository and service interfaces) with in-memory or mocked implementations for unit tests. For EF Core, prefer in-memory providers or explicit SQLite in-memory modes where appropriate.
- **Commit Practice**: Each feature branch should include the failing-test commit (the test authoring step) in the branch history so reviewers can verify the TDD progression. If the failing test is not present, reviewers should request clarification.

### 1. Dependency Injection & Testability

**Core Principle**: All external dependencies are abstracted behind interfaces.

**Pattern**:

- Create interface in `Infrastructure/Services/I<ServiceName>.cs`
- Implement in `Infrastructure/Services/<ServiceName>.cs`
- Decorate with `[Service]` attribute
- Inject via constructor
- Mock in tests via interface

**Example**:

```csharp
// Infrastructure/Services/IGraphApiClient.cs
public interface IGraphApiClient
{
    Task<DriveItem> GetItemAsync(string accountId, string itemId);
}

// Infrastructure/Services/GraphApiClient.cs
[Service]
public class GraphApiClient : IGraphApiClient
{
    public async Task<DriveItem> GetItemAsync(string accountId, string itemId) { }
}
```

### 2. Repository Pattern

Data access is abstracted through repositories:

- **Location**: `Infrastructure/Repositories/`
- **Pattern**: All data operations go through repository interfaces
- **Database Access**: EF Core DbContext via `SyncDbContext`
- **Migrations**: Located in `Infrastructure/Data/Migrations/`

**Repository Principles**:

- One repository per aggregate/entity type
- Methods return domain models, not entities
- DbContext is scoped to service lifetime
- Queries include necessary `Include()` statements for relationships

### 3. Reactive Programming

The application uses **System.Reactive** for asynchronous operations:

**Patterns**:

- `IObservable<T>` for observable sequences
- `BehaviorSubject<T>` for mutable observable state
- ReactiveUI: `ReactiveObject` and `WhenAnyValue()` for ViewModel binding
- `CancellationToken` for cancellation support

**Example**:

```csharp
private readonly BehaviorSubject<SyncState> _progressSubject;

public IObservable<SyncState> Progress => _progressSubject;

_progressSubject.OnNext(newState); // Emit state change
```

### 4. Conflict Detection & Resolution

**Strategy**: Last-Write-Wins based on timestamps

- Remote changes tracked via `cTag` (Microsoft's content tag)
- Local changes tracked via file modification time (`mtime`)
- Allowed time difference threshold: 60 seconds

**Conflict Flow**:

1. Remote change fetched via Graph API
2. Compare with local file timestamp
3. If both changed recently: Record conflict
4. User resolves via UI: Keep local, keep remote, or view both
5. Resolution applied and sync continues

### 5. Sync Algorithm

**Two-Phase Sync Process**:

1. **Download Phase (Remote → Local)**
   - Fetch delta changes using Microsoft Graph delta query
   - Process each remote change
   - Detect conflicts with local changes
   - Apply non-conflicting downloads
   - Save progress (delta token for resumption)

2. **Upload Phase (Local → OneDrive)**
   - Query pending uploads from database
   - Process each local change
   - Upload to OneDrive via Graph API
   - Update local metadata with remote cTag
   - Mark as synced

**For Details**: See `docs/sync-algorithm-overview.md`

### 6. Testing Strategy

**Test Organization**:

- **Unit Tests**: `test/AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit/`
- **Core Tests**: `test/AStar.Dev.OneDrive.Client.Core.Tests.Unit/`
- **Integration Tests**: `test/AStar.Dev.OneDrive.Client.Tests.Integration/`
- **UI Tests**: `test/AStar.Dev.OneDrive.Client.Tests.Unit/`
- **NuGet Package Tests**: `test/nuget-packages/`

**Testing Patterns**:

1. **Naming Convention**: `<ComponentName>Should`

   ```csharp
   public class WindowPreferencesServiceShould { }
   ```

2. **Fact vs Theory**:
   - `[Fact]` - Single test case
   - `[Theory] [InlineData(...)]` - Parameterized tests

3. **In-Memory Database Testing**:

   ```csharp
   using SyncDbContext context = CreateInMemoryContext();
   var service = new WindowPreferencesService(context);
   ```

4. **Mocking Pattern**: Create mocks via interface, use with services

5. **Assertion Library**: Shouldly (fluent assertions)
   ```csharp
   result.ShouldNotBeNull();
   result.ShouldBe(expectedValue);
   ```

### 7. Database & EF Core

**DbContext**: `SyncDbContext` (in `Infrastructure/Data/`)

**Configuration**:

- Uses SQLite for local storage
- Located in platform-specific data directories:
  - **Windows**: `%APPDATA%/astar-dev-onedrive-sync-client/`
  - **Linux**: `~/.local/share/astar-dev-onedrive-sync-client/`
  - **macOS**: `~/Library/Application Support/astar-dev-onedrive-sync-client/`
- Migrations tracked in `Infrastructure/Data/Migrations/`

**Creating Migrations**:

```bash
dotnet ef migrations add MigrationName --project src/AStar.Dev.OneDrive.Client.Infrastructure
```

**Key Entities**:

- Account authentication tokens and metadata
- Synced file metadata and state
- Sync configuration and selections
- Conflict records
- Operation history logs

### 8. Authentication (MSAL)

**Flow**:

1. User initiates login
2. `IAuthenticationClient` prompts OAuth flow
3. MSAL caches tokens securely
4. `IGraphApiClient` uses cached/refreshed tokens for API calls
5. Refresh token renewal: Automatic on expiration

**Multi-Account Support**:

- Each account stored separately in database
- Account ID (from Azure AD) is key identifier
- AuthenticationClient manages per-account token cache

### 9. Configuration & Logging

**Configuration Sources** (in order of precedence):

1. User Secrets (dev environment)
2. `appsettings.json`
3. Environment variables
4. Azure Key Vault (production)

**Logging**:

- **Framework**: Serilog with structured logging
- **Sinks**: Console, async file
- **Log Levels**: Trace → Verbose, Debug, Info, Warning, Error, Fatal
- **Context**: `DebugLogContext` adds account ID to all logs
- **Storage**: Debug logs stored in database and exported

### 10. Source Generators

**Purpose**: Reduce boilerplate and improve compile-time safety

**Generators Available**:

- **ServiceRegistration**: Auto-registers `[Service]` classes
- **OptionsBinding**: Auto-binds config sections to POCO classes

**Example**:

```csharp
[Service(ServiceLifetime.Singleton)]
public class MyService : IMyService { }
// Generates:
// services.AddSingleton<IMyService, MyService>();
```

### 11. Code Style & Quality

**Code Standards**:

- **Language Version**: C# 14 with nullable reference types enabled
- **Warnings as Errors**: `TreatWarningsAsErrors = true`
- **Implicit Usings**: Enabled
- **Naming**: PascalCase (public), camelCase (private), \_prefixWithUnderscore (fields)
- **XML Documentation**: Required on public members

**Best Practices**:

- Use `using` statements and leveraging IDisposable
- Async/await throughout (no sync-over-async)
- Null checking with null-coalescing operators
- Use `ArgumentNullException` in public constructors
- Immutable when possible (records, init properties)
- Avoid magic strings/numbers (use constants or enums)
- Prefer expression-bodied members for simple methods
- Use pattern matching and switch expressions for clarity
- Avoid deep nesting (early returns, guard clauses)
- Use `var` when the type is obvious from the right-hand side, else explicit types for clarity
- Follow SOLID principles and clean architecture guidelines
- Keep methods small and focused (ideally <20 lines)
- Keep classes focused on a single responsibility
- Keep classes and methods cohesive (related functionality grouped together)
- Avoid large constructors (ideally <5 parameters); consider refactoring or using parameter objects if needed
- Use dependency injection for all external dependencies (no newing up services inside classes)
- Do not use regions or #pragma to hide code; refactor instead
- Use `nameof()` for parameter names in exceptions and logging
- Use `ConfigureAwait(false)` in library code to avoid deadlocks in UI contexts
- Use `async` suffix for asynchronous methods (e.g., `GetItemAsync()`)
- Use `CancellationToken` parameters for all async methods that support cancellation
- Use `IAsyncDisposable` and `await using` for async cleanup when necessary
- Use `IEnumerable<T>` for collections that do not require indexing, and `IReadOnlyList<T>` or `IReadOnlyCollection<T>` when immutability is desired
- Use `record` types for immutable data models and DTOs, and `class` for entities with behavior or mutable state
- Use `private` fields with `_camelCase` naming convention, and `PascalCase` for properties and methods
- Use `const` for compile-time constants and `static readonly` for runtime constants
- Use `StringBuilder` for concatenating multiple strings in loops or when performance is a concern, otherwise use string interpolation for readability
- Use `Functional Programming` constructs (e.g., `Result<T>`, `Option<T>`) from the internal `AStar.Dev.Functional.Extensions` package to handle errors and optional values in a more expressive way
- Use `Match<Async>` methods on `Result<T>` and `Option<T>` for handling success and failure cases without throwing exceptions
- Use `Map<Async>` and `Bind<Async>` methods for transforming and chaining operations on `Result<T>` and `Option<T>` types
- Use `Shouldly` for assertions in tests to improve readability and provide better failure messages
- Use `PrimaryConstructor` syntax when possible to reduce boilerplate
- Use `global using` directives for commonly used namespaces to reduce clutter at the top of files
- Use `file-scoped namespaces` for better readability and less indentation
- Use `top-level statements` for simple entry points (e.g., `Program.cs`) to reduce boilerplate
- Use `target-typed new` expressions to reduce redundancy when the type can be inferred from the context
- Use `record struct` for small, immutable value types that do not require reference semantics
- Use `with` expressions to create modified copies of immutable objects
- Always prefer composition over inheritance, and favor interfaces for abstraction
- Use `async`/`await` for asynchronous programming, and avoid blocking calls (e.g., `Task.Wait()`, `Task.Result`) to prevent deadlocks and improve scalability
- Use `Collection Initializers` and `Object Initializers` for cleaner code when creating collections and objects
- Use `Pattern Matching` and `Switch Expressions` for clearer and more concise code when dealing with multiple conditions

---

## File Organization & Conventions

### Folder Structure

```
src/
├── AStar.Dev.OneDrive.Client/           # Presentation Layer
│   ├── Accounts/                        # Account management UI
│   ├── MainWindow/                      # Main window + ViewModel
│   ├── Syncronisation/                  # Sync UI components
│   ├── SyncronisationConflicts/         # Conflict resolution UI
│   ├── DebugLogs/                       # Debug logging viewer
│   ├── Converters/                      # Avalonia value converters
│   ├── Assets/                          # Images, icons, resources
│   ├── ConfigurationSettings/           # App configuration
│   ├── Program.cs                       # Entry point
│   └── App.axaml.cs                     # Application initialization
│
├── AStar.Dev.OneDrive.Client.Infrastructure/  # Infrastructure Layer
│   ├── Services/                        # Business logic services
│   │   ├── Authentication/              # Auth services
│   │   └── OneDriveServices/            # OneDrive-specific services
│   ├── Repositories/                    # Data access
│   ├── Data/                            # EF Core context + migrations
│   └── SerilogLogParser.cs              # Log parsing utilities
│
├── AStar.Dev.OneDrive.Client.Core/      # Core/Domain Layer
│   ├── Models/                          # Domain models
│   ├── Data/
│   │   └── Entities/                    # EF Core entities
│   ├── ApplicationMetadata.cs
│   └── DatabaseConfiguration.cs
│
└── nuget-packages/                      # Internal packages
    ├── AStar.Dev.Logging.Extensions/
    ├── AStar.Dev.Functional.Extensions/
    ├── AStar.Dev.Source.Generators/
    └── AStar.Dev.Utilities/

test/
├── AStar.Dev.OneDrive.Client.Tests.Unit/
├── AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit/
├── AStar.Dev.OneDrive.Client.Core.Tests.Unit/
├── AStar.Dev.OneDrive.Client.Tests.Integration/
└── nuget-packages/                      # Package-specific tests
```

### Naming Conventions

- **Interfaces**: `I<Name>` (e.g., `IGraphApiClient`)
- **Classes**: `<Name>` (e.g., `GraphApiClient`)
- **Methods**: Async methods end with `Async` (e.g., `GetUserAsync()`)
- **Test Classes**: `<ComponentName>Should` (e.g., `SyncEngineShould`)
- **Test Methods**: Descriptive, behavior-focused (e.g., `ReturnNullWhenNoPreferencesExist()`)
- **Fields**: `_camelCaseWithUnderscore` (private)
- **Constants**: `CONSTANT_CASE` or `PascalCase`

---

## Build & Run Commands

### Build

```bash
dotnet build
# Or via task:
dotnet build {{ workspace_folder }}/AStar.Dev.OneDrive.Client.slnx
```

### Run/Debug

```bash
dotnet watch run --project src/AStar.Dev.OneDrive.Client/
# Or via task (watch mode)
```

### Publish (Release Build)

```bash
dotnet publish {{ workspace_folder }}/AStar.Dev.OneDrive.Client.slnx
```

### Run Tests

```bash
dotnet test
# Specific test project:
dotnet test test/AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit/
```

### Entity Framework Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName \
  --project src/AStar.Dev.OneDrive.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Client

# Update database
dotnet ef database update \
  --project src/AStar.Dev.OneDrive.Client.Infrastructure \
  --startup-project src/AStar.Dev.OneDrive.Client
```

---

## Key Implementation Details

### Handling Sealed/Unmockable Classes

**Problem**: Some Microsoft libraries (MSAL, Graph) use sealed classes.

**Solution**: Wrapper interfaces around sealed classes.

**Examples**:

- `IGraphApiClient` wraps `GraphServiceClient`
- `IAuthenticationClient` wraps `IPublicClientApplication`
- `IFileWatcherService` wraps `FileSystemWatcher`

### File Watcher Pattern

**Purpose**: Detect local file system changes in real-time

**Implementation**:

- `FileWatcherService` monitors configured sync directories
- Debounces rapid changes (multiple rapid edits = one sync)
- Triggers immediate local → remote upload

### Delta Query Pattern

**Purpose**: Efficiently fetch only changed items from OneDrive

**Flow**:

1. Fetch changes using Graph `/delta` endpoint
2. Extract `cTag` (content tag) from each item
3. Store delta token for next query
4. Resume from saved token on next sync

### Conflict Resolution Storage

**Storage**: `SyncConflictRepository` persists unresolved conflicts

**User Resolution Options**:

1. **Keep Local**: Discard remote, re-upload local
2. **Keep Remote**: Discard local, re-download remote
3. **View Both**: Inspect both versions, then choose

**Application**: Resolution applied during next sync phase

### Progress Reporting

**Mechanism**: `BehaviorSubject<SyncState>` observable

**Reported Metrics**:

- Total bytes to sync
- Bytes synced
- Files processed
- Current file being processed
- Sync stage (downloading/uploading)

**UI Updates**: ReactiveUI bindings subscribe to `SyncEngine.Progress`

---

## Common Development Tasks

### Adding a New Service

1. **Create Interface**: `Infrastructure/Services/I<ServiceName>.cs`
2. **Implement Service**: `Infrastructure/Services/<ServiceName>.cs`
3. **Add `[Service]` Attribute**: With appropriate lifetime
4. **Inject in Consumers**: Via constructor parameters
5. **Create Tests**: `<ServiceName>Should.cs`
6. **Mock in Tests**: Create test doubles via interface

### Adding a New Repository

1. **Create Interface**: `Infrastructure/Repositories/I<EntityName>Repository.cs`
2. **Implement**: `Infrastructure/Repositories/<EntityName>Repository.cs`
3. **Create Entity**: In `Core/Data/Entities/`
4. **Configure DbContext**: In `SyncDbContext.OnModelCreating()`
5. **Create Migration**: `dotnet ef migrations add Add<Entity>`

### Adding a New ViewModel

1. **Create Class**: Inherit from `ReactiveObject`
2. **Add Properties**: Use reactive properties with `this.WhenAnyValue()`
3. **Create Commands**: Use `ICommand` with reactive binding
4. **Add Tests**: Mock dependencies via interfaces
5. **Create View**: XAML with DataContext binding

### Modifying Database Schema

1. **Edit Entity** in `Core/Data/Entities/`
2. **Update DbContext** configuration if needed
3. **Create Migration**: `dotnet ef migrations add <Name>`
4. **Review Generated Migration**: Verify logic is correct
5. **Apply Migration**: `dotnet ef database update`
6. **Update Repository**: Add new properties/queries

---

## Debugging & Troubleshooting

### Debug Logging

**Access**: DebugLogs view in UI or database query

**Adding Logs**:

```csharp
await DebugLog.EntryAsync(DebugLogMetadata.Services.MyService.MyMethod, accountId, cancellationToken);
await DebugLog.InfoAsync("Source", accountId, "Message", cancellationToken);
await DebugLog.ErrorAsync("Source", accountId, exception, "Message", cancellationToken);
```

### Common Issues

1. **Sync Not Starting**: Check account authentication, sync folder selection
2. **Conflicts Not Detected**: Verify cTag and mtime timestamps
3. **Database Locked**: Close running instance, check migration status
4. **Graph API Errors**: Check token expiration, permissions, rate limits

### Running Specific Tests

```bash
# All tests in a project
dotnet test test/AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit

# Specific test class
dotnet test --filter WindowPreferencesServiceShould

# Watch mode (auto-rerun on changes)
dotnet watch test
```

---

## Performance Considerations

### Sync Performance

- **Delta Queries**: Only fetch changes since last sync (efficient)
- **Batch Operations**: Group API calls when possible
- **Progress Throttling**: Update UI every second, not per-file
- **Cancellation**: Support mid-sync cancellation

### Memory Management

- **Streaming**: Large uploads/downloads streamed, not buffered
- **DbContext Lifetime**: Scoped to service lifetime, cleaned up after use
- **File Handles**: Properly disposed in `using` statements

### Database Performance

- **Indexing**: Entity Framework migrations include necessary indexes
- **Query Optimization**: Include related entities in queries
- **Batch Updates**: Use bulk operations for multiple records

---

## Documentation References

### In-Repo Documentation

- **Sync Algorithm**: `docs/sync-algorithm-overview.md` - Technical details of bidirectional sync
- **Multi-Account UX**: `docs/multi-account-ux-implementation-plan.md` - Account management design
- **Manual Testing**: `docs/sprint-4-manual-testing-guide.md` - QA procedures
- **Debug Logging**: `docs/debug-logging-usage-guide.md` - Logging infrastructure
- **User Manual**: `docs/user-manual.md` - End-user documentation

### External Resources

- **Avalonia**: https://docs.avaloniaui.net/
- **ReactiveUI**: https://www.reactiveui.net/
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **Microsoft Graph API**: https://learn.microsoft.com/en-us/graph/
- **MSAL.NET**: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet
- **xUnit.net**: https://xunit.net/
- **System.Reactive**: https://github.com/Reactive-Extensions/Rx.NET

---

## Contributing Guidelines

### Code Review Checklist

- [ ] Code follows naming conventions and style guide
- [ ] All public members have XML documentation
- [ ] New services/repositories have interfaces
- [ ] Business logic is abstracted behind interfaces
- [ ] Tests cover happy path and error cases
- [ ] No warnings (TreatWarningsAsErrors enabled)
- [ ] Database changes have migrations
- [ ] Async/await used throughout (no sync-over-async)

### Commit Message Style

Use conventional commits:

- `feat: Add new feature`
- `fix: Fix specific bug`
- `refactor: Refactor component`
- `test: Add/update tests`
- `docs: Update documentation`
- `chore: Update dependencies, build changes`

---

## Conclusion

This AStar Dev OneDrive Sync Client demonstrates professional-grade C# development with:

- **Clean Architecture**: Layered design with clear separation of concerns
- **Testability**: Extensive use of interfaces and dependency injection
- **Reactive Patterns**: Modern async/reactive programming with Rx and ReactiveUI
- **Code Generation**: Source generators for boilerplate elimination
- **Database Abstraction**: EF Core with migrations for schema evolution
- **User Experience**: Responsive UI with progress tracking and conflict resolution

When enhancing the codebase, maintain these principles and refer to existing patterns as templates.
