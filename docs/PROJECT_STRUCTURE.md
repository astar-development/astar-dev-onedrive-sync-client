# Project Structure Documentation

## Overview

This document describes the directory structure and organization of the OneDrive Sync Client project, which follows a **vertical-slice, feature-based architecture** combined with layered design principles.

## Directory Layout

```
src/AStar.Dev.OneDrive.Sync.Client/
├── Features/                           # Feature-based organization (vertical slices)
│   ├── Authentication/                 # OAuth, token management, account login
│   ├── AccountManagement/              # Account CRUD, settings, preferences
│   ├── FileSync/                       # Bidirectional file synchronization
│   ├── ConflictResolution/             # Conflict detection and resolution
│   ├── Scheduling/                     # Background sync scheduling
│   ├── Telemetry/                      # Logging, tracing, diagnostics
│   └── LogViewer/                      # Application log viewing UI
│
├── Common/                             # Shared cross-cutting concerns
│   ├── Models/                         # Shared domain models, enums, DTOs
│   ├── Extensions/                     # Extension methods, utilities
│   ├── Constants/                      # Application constants
│   ├── Exceptions/                     # Custom exception types
│   └── Utilities/                      # Helper utilities (hashing, etc.)
│
├── Infrastructure/                     # Cross-cutting infrastructure
│   ├── Database/                       # EF Core, PostgreSQL, migrations
│   ├── SecureStorage/                  # Platform-specific secure storage
│   ├── GraphApi/                       # Microsoft Graph API integration
│   └── Configuration/                  # App settings, DI setup
│
├── Views/                              # Top-level Avalonia UI views
├── App.xaml                            # Application-wide XAML resources
├── Program.cs                          # Application entry point
└── AppModule.cs                        # Dependency injection registration
```

## Feature Slice Structure

Each feature follows a consistent internal structure with all layers represented:

```
Features/[FeatureName]/
├── Controllers/                # UI entry points, commands, event handlers
├── ViewModels/                 # ReactiveUI ViewModels for UI binding
├── Services/                   # Business logic orchestration
├── Models/                     # Feature-specific domain models and value objects
├── Repositories/               # Data access layer for the feature
└── [SubfolderName]/            # Feature-specific subfolders (e.g., OAuth, DeltaSync)
```

### Example: Authentication Feature

```
Features/Authentication/
├── Controllers/                # Auth command handlers
├── ViewModels/
│   ├── AddAccountViewModel.cs
│   └── EditAccountViewModel.cs
├── Services/
│   └── AuthenticationService.cs
├── Models/
│   ├── Account.cs
│   └── AuthToken.cs
├── Repositories/
│   └── AccountRepository.cs
├── OAuth/                      # OAuth-specific implementations
│   ├── MsalTokenProvider.cs
│   └── TokenRefreshScheduler.cs
└── Tests/                      # (in separate test project)
    ├── AuthenticationServiceTests.cs
    └── AccountRepositoryTests.cs
```

## Common Folder

Shared concerns used across multiple features:

- **Models/**: Shared DTOs, enums, value objects (e.g., `PagedResult<T>`, `SyncStatus`)
- **Extensions/**: String extensions, collection extensions, etc.
- **Constants/**: Application-wide constants (schema names, timeouts, limits)
- **Exceptions/**: Custom exceptions inheriting from `Exception` (e.g., `SyncException`, `ConflictException`)
- **Utilities/**: Hashing services, file utilities, etc.

## Infrastructure Folder

Low-level technical implementations:

- **Database/DbContext/**: `OneDriveSyncDbContext`, entity configurations
- **Database/Migrations/**: EF Core migrations (auto-generated)
- **Database/Seeding/**: Initial data seeding logic
- **SecureStorage/**: `ISecureTokenStorage` implementations (Windows DPAPI, macOS Keychain, Linux SecretService)
- **GraphApi/**: Kiota-generated Graph API client, factories, configurations
- **Configuration/**: `Program.cs` setup, `IServiceCollection` extensions, settings

## Layered Architecture

Each feature implements these layers (as applicable):

1. **Presentation Layer** (Controllers/ViewModels)
   - AvaloniaUI Views bind to ReactiveUI ViewModels
   - Command handlers trigger business logic
   - Reactive properties expose state to UI

2. **Application Layer** (Services)
   - Orchestrate workflow across multiple repositories
   - Handle business rules and use cases
   - Coordinate between features

3. **Domain Layer** (Models)
   - Aggregate roots, entities, value objects
   - Business logic encapsulation
   - Minimal external dependencies

4. **Infrastructure Layer** (Repositories, Database, External Services)
   - Data persistence via EF Core
   - External system integration (Graph API, secure storage)
   - Technical implementation details

## Testing Structure

Tests are organized in a separate project: `test/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/`

```
test/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/
├── Features/
│   ├── Authentication/
│   │   ├── AuthenticationServiceTests.cs
│   │   ├── AccountRepositoryTests.cs
│   │   └── AddAccountViewModelTests.cs
│   ├── FileSync/
│   │   ├── FileSyncServiceTests.cs
│   │   └── DeltaSyncServiceTests.cs
│   └── ...
├── Common/
│   ├── Utilities/
│   │   └── HashingServiceTests.cs
│   └── Extensions/
│       └── StringExtensionsTests.cs
└── Fixtures/
    ├── DatabaseFixture.cs        # In-memory DB for testing
    ├── GraphApiMocks.cs          # Mock Graph API responses
    └── SecureStorageMocks.cs     # Mock secure storage
```

**Testing Tools:**
- **xUnit**: Test framework
- **NSubstitute**: Mocking and test doubles
- **Shouldly**: Assertion library
- **SpecFlow**: BDD feature files (for integration/acceptance tests)
- **Testcontainers**: PostgreSQL container for integration tests

## Naming Conventions

### Namespaces
- Feature namespaces: `AStar.Dev.OneDrive.Sync.Client.Features.Authentication`
- Common namespaces: `AStar.Dev.OneDrive.Sync.Client.Common.Utilities`
- Infrastructure namespaces: `AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database`

### Classes
- Services: `[Feature]Service` (e.g., `AuthenticationService`)
- Repositories: `[Entity]Repository` (e.g., `AccountRepository`)
- ViewModels: `[Feature]ViewModel` or `[Action][Feature]ViewModel` (e.g., `AddAccountViewModel`)
- Views: `[Feature]View.axaml` (e.g., `AddAccountView.axaml`)
- Models/Entities: Singular form (e.g., `Account`, `FileSystemItem`)

## Key Design Principles

1. **Vertical Slices**: Each feature is independently deployable and testable
2. **Layered Architecture**: Clear separation of concerns within each feature
3. **Dependency Injection**: All dependencies injected via constructor
4. **Reactive UI**: ReactiveUI for responsive, composable UI logic
5. **Entity Framework Core**: Repository pattern for data access
6. **GDPR Compliance**: Data hashing, no PII in database
7. **Cross-Platform**: Platform-specific implementations behind interfaces

## Folder Creation Checklist

- [x] Features/ with all 7 feature slices
- [x] Each feature with Controllers/, ViewModels/, Services/, Models/, Repositories/
- [x] FileSync with additional subfolders (DeltaSync, LocalFileOperations, UploadQueue, DownloadQueue)
- [x] Common/ with Models/, Extensions/, Constants/, Exceptions/, Utilities/
- [x] Infrastructure/ with Database/, SecureStorage/, GraphApi/, Configuration/
- [x] Database subfolder with DbContext/, Migrations/, Seeding/

## Next Steps

1. Implement Task 1.2: Configure Dependency Injection
2. Implement Task 1.3-1.5: Add NuGet packages
3. Begin Task 1.6: Create DbContext
