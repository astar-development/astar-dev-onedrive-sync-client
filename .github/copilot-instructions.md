# Copilot Instructions - AStar Dev OneDrive Sync Client

## Project Overview

**AStar Dev OneDrive Sync Client** is a cross-platform desktop application providing bidirectional synchronization between local file systems and Microsoft OneDrive with intelligent conflict detection, multi-account support, and efficient delta-based synchronization.

**Tech Stack**: C# 14, .NET 10.0, Avalonia 11.3.11 + ReactiveUI, SQLite + EF Core 10.0.2, MSAL 4.81.0, Microsoft Graph API 5.101.0, System.Reactive 6.1.0, xUnit

**Quick Start**: See [docs/quick-start.md](../docs/quick-start.md) for build and run commands.

---

## Architecture Overview

<CRITICAL_REQUIREMENT type="MANDATORY">
Strict layered architecture: Presentation → Infrastructure → Core/Domain. Core has NO dependencies on other layers. All cross-layer communication via interfaces. Infrastructure depends ONLY on Core. Presentation depends on Infrastructure and Core.
</CRITICAL_REQUIREMENT>

### Layers

**1. Presentation** (`AStar.Dev.OneDrive.Sync.Client`): Avalonia XAML views, ReactiveUI ViewModels, converters, behaviors

**2. Infrastructure** (`AStar.Dev.OneDrive.Sync.Client.Infrastructure`): Services, Repositories, Data Access, External API integrations

**3. Core/Domain** (`AStar.Dev.OneDrive.Sync.Client.Core`): Domain models, EF Core entities, enums (no external dependencies)

### Dependency Injection

Uses **Microsoft.Extensions.DependencyInjection** with custom source generators:

- Decorate services with `[Service(ServiceLifetime, As = typeof(IInterface))]`
- Auto-generates `ServiceCollectionExtensions` via `AStar.Dev.Source.Generators`
- Access via `App.Host.Services`

### Custom NuGet Packages

- **AStar.Dev.Functional.Extensions**: `Result<T>`, `Option<T>`, functional utilities
- **AStar.Dev.Logging.Extensions**: Structured logging helpers
- **AStar.Dev.Source.Generators**: Service registration and options binding code generation
- **AStar.Dev.Utilities**: Common utilities (JSON, regex, string extensions)

---

## Development Approach & Patterns

### Test-Driven Development (TDD)

<CRITICAL_REQUIREMENT type="MANDATORY">
All changes must follow Red → Green → Refactor cycle. Failing tests must be committed to the branch history to demonstrate TDD progression. Tests should be small, focused, and behavior-driven. Use mocks and test doubles for external dependencies. Integration tests for cross-service flows, E2E tests for critical user-facing scenarios.
</CRITICAL_REQUIREMENT>

<COMMIT_REQUIREMENTS type="MANDATORY">

- Each feature branch must include the failing-test commit in branch history
- Use conventional commit format: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`
- Reviewers should request clarification if failing test commits are absent
  </COMMIT_REQUIREMENTS>

### Quality & Coverage Policy

<CRITICAL_REQUIREMENT type="MANDATORY">

- **Minimum**: 80% branch coverage for new code
- **Critical paths**: 100% coverage (authentication, sync engine, conflict resolution)
- **Test pyramid**: 70% unit, 20% integration, 10% E2E
  </CRITICAL_REQUIREMENT>

### Branching & Pull Requests

<WORKFLOW_ENFORCEMENT type="MANDATORY">

- **Strategy**: Trunk-based development with short-lived feature branches (1-2 days)
- **Main branch**: Always deployable, all tests passing, no critical bugs
- **PR size**: < 300 lines of code changed, < 20 files
- **Large changes**: Use feature flags (disabled by default) until complete
- **Review**: At least one approval required; respond within 12 hours
- **Merge**: Squash and merge within 24 hours when possible
- **CI gates**: All tests pass, no warnings, linter clean
  </WORKFLOW_ENFORCEMENT>

<NAMING_REQUIREMENTS type="MANDATORY">

- **Feature branches**: `feature/<descriptive-name>` (e.g., `feature/add-file-watcher-service`)
- **Bug fixes**: `fix/<descriptive-name>` (e.g., `fix/resolve-sync-conflict`)
- **Refactors**: `refactor/<descriptive-name>` (e.g., `refactor/extract-repository-interfaces`)
- **Documentation**: `docs/<descriptive-name>`
  </NAMING_REQUIREMENTS>

**Full Contributing Guidelines**: See [.github/CONTRIBUTING.md](CONTRIBUTING.md)

---

## Key Development Patterns

### 1. Dependency Injection & Testability

<CODING_REQUIREMENTS type="MANDATORY">
All external dependencies must be abstracted behind interfaces. Create interface in `Infrastructure/Services/I<ServiceName>.cs`, implement in `Infrastructure/Services/<ServiceName>.cs`, decorate with `[Service]` attribute, inject via constructor, mock in tests via interface.
</CODING_REQUIREMENTS>

### 2. Repository Pattern

<PROCESS_REQUIREMENTS type="MANDATORY">

- Location: `Infrastructure/Repositories/`
- One repository per aggregate/entity type
- Methods return domain models, not entities
- DbContext scoped to service lifetime
- Queries include necessary `Include()` for relationships
  </PROCESS_REQUIREMENTS>

### 3. Reactive Programming

Uses **System.Reactive** for asynchronous operations:

- `IObservable<T>` for observable sequences
- `BehaviorSubject<T>` for mutable observable state
- ReactiveUI: `ReactiveObject` and `WhenAnyValue()` for ViewModel binding
- `CancellationToken` for cancellation support

### 4. Sync Algorithm

**Two-Phase Sync Process**:

1. **Download Phase (Remote → Local)**: Fetch delta changes, detect conflicts, apply downloads, save delta token
2. **Upload Phase (Local → OneDrive)**: Query pending uploads, upload to OneDrive, update metadata, mark synced

**Details**: See [docs/sync-algorithm-overview.md](../docs/sync-algorithm-overview.md)

### 5. Code Style & Quality

<CODING_REQUIREMENTS type="MANDATORY">

- Follow [.github/instructions/style-guidelines.instructions.md](instructions/style-guidelines.instructions.md) for comprehensive C# coding standards
- **Warnings as Errors**: `TreatWarningsAsErrors = true`
- **XML Documentation**: Required on public members; never on private members or tests
- **Modern C# Features**: Primary constructors, file-scoped namespaces, target-typed new, `record struct`
- **Async/Await**: Throughout; never sync-over-async (`Task.Wait()`, `Task.Result`)
- **Functional Programming**: Use `Result<T>` and `Option<T>` from `AStar.Dev.Functional.Extensions`
  </CODING_REQUIREMENTS>

**Implementation Details**: See [.github/instructions/implementation-patterns.instructions.md](instructions/implementation-patterns.instructions.md)

---

## File Organization & Conventions

<NAMING_REQUIREMENTS type="MANDATORY">

- **Interfaces**: `I<Name>` (e.g., `IGraphApiClient`)
- **Classes**: `<Name>` (e.g., `GraphApiClient`)
- **Methods**: Async methods end with `Async` (e.g., `GetUserAsync()`)
- **Test Classes**: `<ComponentName>Should` (e.g., `SyncEngineShould`)
- **Test Methods**: Descriptive, behavior-focused (e.g., `ReturnNullWhenNoPreferencesExist()`)
- **Fields**: `_camelCaseWithUnderscore` (private)
- **Constants**: `CONSTANT_CASE` or `PascalCase`

Full style guidelines: [.github/instructions/style-guidelines.instructions.md](instructions/style-guidelines.instructions.md)
</NAMING_REQUIREMENTS>

### Folder Structure

```
src/
├── AStar.Dev.OneDrive.Sync.Client/           # Presentation Layer
├── AStar.Dev.OneDrive.Sync.Client.Infrastructure/  # Infrastructure Layer
├── AStar.Dev.OneDrive.Sync.Client.Core/      # Core/Domain Layer
└── nuget-packages/                           # Internal packages

test/
├── AStar.Dev.OneDrive.Sync.Client.Tests.Unit/
├── AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/
├── AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit/
└── AStar.Dev.OneDrive.Sync.Client.Tests.Integration/
```

---

## Common Tasks & Guides

- **Development Tasks**: [.github/instructions/development-tasks.instructions.md](instructions/development-tasks.instructions.md)
  - Adding a New Service
  - Adding a New Repository
  - Adding a New ViewModel
  - Modifying Database Schema

- **Implementation Patterns**: [.github/instructions/implementation-patterns.instructions.md](instructions/implementation-patterns.instructions.md)
  - Handling Sealed/Unmockable Classes
  - File Watcher Pattern
  - Delta Query Pattern
  - Conflict Resolution Storage
  - Progress Reporting

- **Troubleshooting**: [.github/instructions/troubleshooting.instructions.md](instructions/troubleshooting.instructions.md)
  - Debug Logging
  - Common Issues
  - Running Specific Tests

- **Performance**: [.github/instructions/performance.instructions.md](instructions/performance.instructions.md)
  - Sync Performance
  - Memory Management
  - Database Performance

- **Quick Start**: [docs/quick-start.md](../docs/quick-start.md)
  - Build & Run Commands
  - Entity Framework Migrations
  - Development Workflow

---

## Documentation References

### In-Repo Documentation

- **Sync Algorithm**: [docs/sync-algorithm-overview.md](../docs/sync-algorithm-overview.md) - Technical details of bidirectional sync
- **Multi-Account UX**: [docs/multi-account-ux-implementation-plan.md](../docs/multi-account-ux-implementation-plan.md) - Account management design
- **Manual Testing**: [docs/sprint-4-manual-testing-guide.md](../docs/sprint-4-manual-testing-guide.md) - QA procedures
- **Debug Logging**: [docs/debug-logging-usage-guide.md](../docs/debug-logging-usage-guide.md) - Logging infrastructure
- **User Manual**: [docs/user-manual.md](../docs/user-manual.md) - End-user documentation

### Instruction Files

- **Style Guidelines**: [.github/instructions/style-guidelines.instructions.md](instructions/style-guidelines.instructions.md) - C# coding standards
- **Backend Guidelines**: [.github/instructions/backend.instructions.md](instructions/backend.instructions.md) - Backend development patterns
- **Frontend Guidelines**: [.github/instructions/frontend.instructions.md](instructions/frontend.instructions.md) - UI/Frontend patterns
- **BDD Tests**: [.github/instructions/bdd-tests.instructions.md](instructions/bdd-tests.instructions.md) - Behavior-driven testing
- **Documentation**: [.github/instructions/docs.instructions.md](instructions/docs.instructions.md) - Documentation standards
- **Development Tasks**: [.github/instructions/development-tasks.instructions.md](instructions/development-tasks.instructions.md) - Step-by-step task guides
- **Implementation Patterns**: [.github/instructions/implementation-patterns.instructions.md](instructions/implementation-patterns.instructions.md) - Project-specific patterns
- **Troubleshooting**: [.github/instructions/troubleshooting.instructions.md](instructions/troubleshooting.instructions.md) - Debugging and common issues
- **Performance**: [.github/instructions/performance.instructions.md](instructions/performance.instructions.md) - Performance optimization

### External Resources

- **Avalonia**: https://docs.avaloniaui.net/
- **ReactiveUI**: https://www.reactiveui.net/
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **Microsoft Graph API**: https://learn.microsoft.com/en-us/graph/
- **MSAL.NET**: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet
- **xUnit.net**: https://xunit.net/
- **System.Reactive**: https://github.com/Reactive-Extensions/Rx.NET

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
