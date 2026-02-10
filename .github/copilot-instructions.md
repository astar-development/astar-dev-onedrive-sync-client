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

1. **Naming Convention**: `<ComponentName>Should` or `<ComponentName>Tests`

   ```csharp
   public class WindowPreferencesServiceShould { }
   public class PatternTests { }
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