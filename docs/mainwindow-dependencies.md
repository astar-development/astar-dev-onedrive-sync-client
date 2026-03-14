# MainWindow Class Dependencies

Source root: `src/AStar.Dev.OneDrive.Sync.Client/Home/MainWindowViewModel.cs`

This document maps dependencies from the `MainWindow`.

## Dependency Tree

```mermaid
flowchart TD
    A{MainWindow.axaml} --> B[MainWindow.axaml.cs]
    A{MainWindow.axaml} --> C[MainWindowViewModel.cs]
    B --> C
    A --> D[ConflictResolutionView.axaml]
    A --> E[AccountManagementView.axaml]
    A --> F[SyncTreeView.axaml]
    A --> G[SyncProgressView.axaml]
    B --> H[IWindowPreferencesService]
    C --> I[AccountManagementViewModel.cs]
    C --> J[SyncTreeViewModel.cs]
    C --> K[IServiceProvider.cs]
    C --> J[IAutoSyncCoordinator.cs]
    C --> L[IAccountRepository.cs]
    C --> M[ISyncConflictRepository.cs]
    C --> N[SettingsViewModel.cs]
    E --> O[AccountManagementView.axaml.cs]
    O --> P[AccountManagementViewModel.cs]
```

## Notes

- Only types created in the `AStar.Dev.OneDrive.Sync.Client` project, it's related projects or any AStar NuGet packages that are *directly* included here will be added to this diagram. Framework or other NuGet packages are not included by design.

Want to go further?

I can extend this script to include:

```text
Namespace‑level grouping

Color‑coded nodes (Views, ViewModels, Services)

Reverse dependency lookup

Graph filtering (e.g., only show MainWindow subtree)

Clickable links to source files

Avalonia DataTemplates → ViewModel mappings

Service registration scanning (from DI container)
```

```text
Add color‑coded Mermaid nodes (Views, ViewModels, Services)

Add namespace grouping (Mermaid subgraphs)

Add reverse dependency lookup (e.g., "what depends on MainWindow?")

Add filtering so you can generate a graph starting only from MainWindow
```

But first — get the script running.
