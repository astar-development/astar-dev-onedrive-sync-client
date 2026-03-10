# MainWindow Class Dependencies (3 Levels)

Source root: `src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow.axaml.cs`

This document maps dependencies from `MainWindow` to a depth of 3 levels.

## Dependency Tree

- Level 0: `MainWindow`
  - File: `src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow.axaml.cs`

- Level 1 (direct dependencies used by `MainWindow`)
  - `MainWindowViewModel`
    - File: `src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindowViewModel.cs`
    - Usage: resolved from `App.Host.Services.GetRequiredService<MainWindowViewModel>()`
  - `IWindowPreferencesService`
    - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/IWindowPreferencesService.cs`
    - Usage: resolved from `App.Host.Services.GetRequiredService<IWindowPreferencesService>()`
  - `WindowPreferences`
    - File: `src/AStar.Dev.OneDrive.Sync.Client.Core/Models/WindowPreferences.cs`
    - Usage: loaded/saved in `LoadWindowPreferencesAsync()` and `SaveWindowPreferencesAsync()`
  - `App.Host` / host service provider entry point
    - File: `src/AStar.Dev.OneDrive.Sync.Client/App.axaml.cs`
    - Host builder: `src/AStar.Dev.OneDrive.Sync.Client/AppHost.cs`

- Level 2 (dependencies of Level 1 classes)
  - From `MainWindowViewModel` constructor
    - `AccountManagementViewModel`
      - File: `src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewModel.cs`
    - `SyncTreeViewModel`
      - File: `src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewModel.cs`
    - `IServiceProvider`
      - Source: .NET DI runtime service provider
    - `IAutoSyncCoordinator`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/IAutoSyncCoordinator.cs`
    - `IAccountRepository`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Repositories/IAccountRepository.cs`
    - `ISyncConflictRepository`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Repositories/ISyncConflictRepository.cs`
  - From `WindowPreferencesService` (implementation of `IWindowPreferencesService`)
    - `SyncDbContext`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Data/SyncDbContext.cs`
    - `WindowPreferencesEntity`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Core/Data/Entities/WindowPreferencesEntity.cs`
    - `ThemePreference`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Core/Models/Enums/ThemePreference.cs`
  - From `App.Host`
    - `AppHost.BuildHost()`
      - File: `src/AStar.Dev.OneDrive.Sync.Client/AppHost.cs`

- Level 3 (dependencies of Level 2 classes)
  - From `AccountManagementViewModel`
    - `IAuthService`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/Authentication/IAuthService.cs`
    - `IAccountRepository`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Repositories/IAccountRepository.cs`
    - `ILogger<AccountManagementViewModel>`
      - Source: Microsoft.Extensions.Logging
  - From `SyncTreeViewModel`
    - `IFolderTreeService`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/OneDriveServices/IFolderTreeService.cs`
    - `ISyncSelectionService`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/ISyncSelectionService.cs`
    - `ISyncEngine`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/ISyncEngine.cs`
    - `IDebugLogger`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/IDebugLogger.cs`
    - `ISyncRepository`
      - File: `src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Repositories/ISyncRepository.cs`
  - From `SyncDbContext`
    - `DbContext` / `DbSet<T>` / EF Core model configuration APIs
      - Source: `Microsoft.EntityFrameworkCore`
  - From `AppHost.BuildHost()` service configuration
    - `AddDatabaseServices()`, `AddAuthenticationServices()`, `AddApplicationServices()`, `AddViewModels()`, `AddAnnotatedServices()`, `AddHostedService<LogCleanupBackgroundService>()`
      - Source: service registration pipeline in `src/AStar.Dev.OneDrive.Sync.Client/AppHost.cs`

## Notes

- This map is intentionally limited to the first 3 levels.
- Framework types used directly in `MainWindow` (for example `Window`, `DispatcherTimer`, `PixelPoint`) are omitted from expansion because they do not introduce project-internal dependency branches.
