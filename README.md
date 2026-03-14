# AStar Dev OneDrive Sync Client

## Project Overview

**AStar Dev OneDrive Sync Client** is a cross-platform desktop application providing bidirectional synchronization between local file systems and Microsoft OneDrive with intelligent conflict detection, multi-account support, and efficient delta-based synchronization.

## Tech Stack

- **Language**: C# 14
- **Framework**: .NET 10.0
- **UI Framework**: Avalonia 11.3.11 + ReactiveUI
- **Database**: SQLite + EF Core 10.0.2
- **Authentication**: MSAL 4.81.0
- **API**: Microsoft Graph API 5.101.0
- **Reactive Programming**: System.Reactive 6.1.0
- **Testing**: xUnit

## Custom NuGet Packages

- **AStar.Dev.Functional.Extensions**: Provides `Result<T>`, `Option<T>`, and functional utilities
- **AStar.Dev.Logging.Extensions**: Structured logging helpers
- **AStar.Dev.Source.Generators**: Service registration and options binding code generation
- **AStar.Dev.Utilities**: Common utilities (JSON, regex, string extensions)

## Architecture

The project follows a strict layered architecture to ensure separation of concerns and maintainability:

- **Presentation Layer** (`AStar.Dev.OneDrive.Sync.Client`): Avalonia XAML views, ReactiveUI ViewModels, converters, behaviors
- **Infrastructure Layer** (`AStar.Dev.OneDrive.Sync.Client.Infrastructure`): Services, Repositories, Data Access, External API integrations
- **Core/Domain Layer** (`AStar.Dev.OneDrive.Sync.Client.Core`): Domain models, EF Core entities, enums (no external dependencies)

Core has NO dependencies on other layers. All cross-layer communication via interfaces. Infrastructure depends ONLY on Core. Presentation depends on Infrastructure and Core.

## Dependency Injection

Uses **Microsoft.Extensions.DependencyInjection** with custom source generators:

- Decorate services with `[Service(ServiceLifetime, As = typeof(IInterface))]`
- Auto-generates `ServiceCollectionExtensions` via `AStar.Dev.Source.Generators`
- Access via `App.Host.Services`

## Project Methodologies

### Test-Driven Development (TDD)

All changes must follow Red → Green → Refactor cycle. Failing tests must be committed to the branch history to demonstrate TDD progression. Tests should be small, focused, and behavior-driven. Use mocks and test doubles for external dependencies. Integration tests for cross-service flows, E2E tests for critical user-facing scenarios.

### Quality & Coverage Policy

- **Minimum**: 80% branch coverage for new code
- **Critical paths**: 100% coverage (authentication, sync engine, conflict resolution)
- **Test pyramid**: 70% unit, 20% integration, 10% E2E

### Branching & Pull Requests

- **Strategy**: Trunk-based development with short-lived feature branches (1-2 days)
- **Main branch**: Always deployable, all tests passing, no critical bugs
- **PR size**: < 300 lines of code changed, < 20 files
- **Large changes**: Use feature flags (disabled by default) until complete
- **Review**: At least one approval required; respond within 12 hours
- **Merge**: Squash and merge within 24 hours when possible
- **CI gates**: All tests pass, no warnings, linter clean

### Naming Requirements

- **Feature branches**: `feature/<descriptive-name>` (e.g., `feature/add-file-watcher-service`)
- **Bug fixes**: `fix/<descriptive-name>` (e.g., `fix/resolve-sync-conflict`)
- **Refactors**: `refactor/<descriptive-name>` (e.g., `refactor/extract-repository-interfaces`)
- **Documentation**: `docs/<descriptive-name>`

### Key Development Patterns

#### 1. Dependency Injection & Testability

All external dependencies must be abstracted behind interfaces. Create interface in `Infrastructure/Services/I<ServiceName>.cs`, implement in `Infrastructure/Services/<ServiceName>.cs`, decorate with `[Service]` attribute, inject via constructor, mock in tests via interface.

#### 2. Repository Pattern

- Location: `Infrastructure/Repositories/`
- One repository per aggregate/entity type
- Methods return domain models, not entities
- DbContext scoped to service lifetime
- Queries include necessary `Include()` for relationships

#### 3. Reactive Programming

Uses **System.Reactive** for asynchronous operations:

- `IObservable<T>` for observable sequences
- `BehaviorSubject<T>` for mutable observable state
- ReactiveUI: `ReactiveObject` and `WhenAnyValue()` for ViewModel binding
- `CancellationToken` for cancellation support

#### 4. Sync Algorithm

**Two-Phase Sync Process**:

1. **Download Phase (Remote → Local)**: Fetch delta changes, detect conflicts, apply downloads, save delta token
2. **Upload Phase (Local → OneDrive)**: Query pending uploads, upload to OneDrive, update metadata, mark synced

## Coding Standards

- Follow [.github/instructions/style-guidelines.instructions.md](.github/instructions/style-guidelines.instructions.md) for comprehensive C# coding standards
- **Warnings as Errors**: `TreatWarningsAsErrors = true`
- **XML Documentation**: Required on public members; never on private members or tests
- **Modern C# Features**: Primary constructors, file-scoped namespaces, target-typed new, `record struct`
- **Async/Await**: Throughout; never sync-over-async (`Task.Wait()`, `Task.Result`)
- **Functional Programming**: Use `Result<T>` and `Option<T>` from `AStar.Dev.Functional.Extensions`

## Project Structure

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

## Getting Started

See [docs/quick-start.md](docs/quick-start.md) for build and run commands.

## Contributing

See [.github/CONTRIBUTING.md](.github/CONTRIBUTING.md) for full contributing guidelines.

## Documentation References

- **Sync Algorithm**: [docs/sync-algorithm-overview.md](docs/sync-algorithm-overview.md)
- **Multi-Account UX**: [docs/multi-account-ux-implementation-plan.md](docs/multi-account-ux-implementation-plan.md)
- **Manual Testing**: [docs/sprint-4-manual-testing-guide.md](docs/sprint-4-manual-testing-guide.md)
- **Debug Logging**: [docs/debug-logging-usage-guide.md](docs/debug-logging-usage-guide.md)
- **User Manual**: [docs/user-manual.md](docs/user-manual.md)