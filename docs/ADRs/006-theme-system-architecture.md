---
title: "ADR-006: Theme System Architecture"
status: "Accepted"
date: "2026-02-13"
authors: "Development Team"
tags: ["architecture", "ui", "themes", "user-experience"]
supersedes: ""
superseded_by: ""
---

# ADR-006: Theme System Architecture

## Status

**Accepted** (Implemented February 2026)

## Context

The application required a theme system to provide users with visual customization options. Key requirements included:

- **Runtime Theme Switching**: Users should be able to change themes without restarting the application
- **Multiple Theme Options**: Support for 6+ distinct visual themes (Original Auto/Light/Dark, Professional, Colourful, Terminal)
- **Theme Persistence**: User's theme selection must persist across application restarts
- **Minimal Code Changes**: Implementation should not require refactoring existing ViewModels or business logic
- **Maintainability**: Adding new themes should be straightforward for future developers
- **Performance**: Theme switching should be instant with negligible performance overhead

The application uses Avalonia 11.3.11 with ReactiveUI for MVVM architecture. The existing codebase has a single MainWindow with multiple user controls for different views (Accounts, SyncTree, ConflictResolution, etc.).

Three architectural approaches were evaluated (detailed in [docs/design/options-for-ui-changes.md](../design/options-for-ui-changes.md)):

1. **Separate Window Classes** - Create distinct MainWindow implementations for each theme
2. **Dynamic Control Templates** - Swap ControlTemplates at runtime
3. **ResourceDictionary Merging** - Swap XAML ResourceDictionaries containing styles and colors

## Decision

We chose **ResourceDictionary Merging** (Option 3) as the architecture for the theme system.

**Implementation Details**:

- **Theme Service**: Singleton ThemeService manages current theme state and switching logic
- **Resource Files**: Each theme defined as standalone .axaml ResourceDictionary in Themes/ folder
- **Runtime Switching**: Application.Current.Resources.MergedDictionaries updated when theme changes
- **Persistence**: WindowPreferencesService saves theme selection to SQLite database
- **UI Integration**: Settings window with ComboBox for theme selection, auto-apply on change
- **Enum-Based**: ThemePreference enum ensures type safety and compile-time validation

**Architecture Flow**:
1. User selects theme from Settings → SettingsViewModel
2. ViewModel calls ThemeService.ApplyThemeAsync(ThemePreference theme)
3. ThemeService loads appropriate .axaml file via AvaloniaXamlLoader.Load()
4. ResourceDictionary merged into Application.Current.Resources.MergedDictionaries
5. Theme preference persisted to database via WindowPreferencesService.SaveAsync()
6. On next application startup, theme restored from database

## Consequences

### Positive

- **POS-001**: **Zero ViewModel Changes** - No modifications required to existing ViewModels, business logic, or data bindings
- **POS-002**: **Instant Theme Switching** - ResourceDictionary merging provides sub-100ms theme application with immediate visual feedback
- **POS-003**: **Easy Theme Development** - New themes require only a new .axaml file; no C# code changes needed
- **POS-004**: **Clean Separation of Concerns** - Visual styling completely decoupled from functional logic
- **POS-005**: **Type Safety** - Enum-based theme selection prevents runtime errors from invalid theme names
- **POS-006**: **Persistence Built-In** - Leverages existing WindowPreferences infrastructure for database storage
- **POS-007**: **Testability** - Integration tests can verify theme persistence across application sessions using in-memory SQLite
- **POS-008**: **Performance** - Negligible memory overhead (~50KB per theme dictionary), no performance impact during normal operation

### Negative

- **NEG-001**: **Resource Key Consistency** - All themes must define the same resource keys; missing keys cause runtime binding failures
- **NEG-002**: **XAML Duplication** - Each theme contains full style definitions, leading to some duplication across .axaml files
- **NEG-003**: **Limited Compile-Time Validation** - ResourceDictionary merging errors only detected at runtime (mitigated by integration tests)
- **NEG-004**: **Memory Usage** - All theme ResourceDictionaries loaded into memory on startup (acceptable given small file sizes)

## Alternatives Considered

### Separate Window Classes

- **ALT-001**: **Description**: Create MainWindow, MainWindowProfessional, MainWindowColourful, MainWindowTerminal classes with distinct XAML definitions
- **ALT-002**: **Pros**: Complete isolation between themes, no resource key conflicts, strong typing
- **ALT-003**: **Cons**: Requires recreating window instance on theme change (disrupts user workflow), state transfer complexity, significant code duplication across window classes
- **ALT-004**: **Rejection Reason**: Poor user experience due to window recreation; high maintenance burden from duplicated ViewModels and event handlers

### Dynamic Control Templates

- **ALT-005**: **Description**: Define ControlTemplates for each theme, swap via ControlTemplate property at runtime
- **ALT-006**: **Pros**: Granular control per-control, supports partial theme customization
- **ALT-007**: **Cons**: Requires modifying every control in XAML to support templating, complex state management, limited to controls (not brushes/colors/fonts)
- **ALT-008**: **Rejection Reason**: Massive refactoring required across entire codebase; doesn't support application-wide color schemes effectively

## Implementation Notes

- **IMP-001**: **Resource Key Convention**: All themes must implement these mandatory resource keys:
  - PrimaryBackgroundBrush, SecondaryBackgroundBrush, AccentBrush
  - PrimaryTextBrush, SecondaryTextBrush
  - ButtonStyle, TextBoxStyle, ListBoxStyle, ComboBoxStyle
- **IMP-002**: **Theme Validation**: Integration tests (ThemePersistenceShould.cs) verify all 6 themes can be applied and persisted
- **IMP-003**: **Fallback Handling**: If theme file fails to load, application defaults to OriginalAuto theme
- **IMP-004**: **Migration Strategy**: Existing users without saved preference default to OriginalAuto to maintain current behavior
- **IMP-005**: **Future Extensibility**: Custom user themes could be supported by allowing users to provide .axaml files in application data directory

## References

- **REF-001**: [Theme Design Options](../design/options-for-ui-changes.md) - Full evaluation of all three architectural approaches
- **REF-002**: [User Manual - Changing Themes](../user-manual.md#changing-themes) - End-user documentation for theme switching
- **REF-003**: [Implementation Plan](../plans/new-themes.md) - Phased development plan for theme system
- **REF-004**: [Avalonia ResourceDictionary Documentation](https://docs.avaloniaui.net/docs/guides/styles-and-resources/resources) - Official Avalonia guidance on resource merging
- **REF-005**: ThemeService.cs - Core service implementation
- **REF-006**: ThemePersistenceShould.cs - Integration test suite validating theme persistence

<!-- © Capgemini 2026 -->
