# Plan: Multi-Theme User Preference System (Runtime Switching)

**TL;DR:** Implement a user-selectable theme system supporting 6 themes: Original (Light/Dark/Auto), Professional, Colourful, and Terminal with **runtime theme switching** (no app restart required). The solution extends the existing WindowPreferences system with a new `ThemePreference` enum, extracts styles from the existing themed MainWindow files into ResourceDictionaries, creates a ThemeService to dynamically swap themes, and adds a settings UI accessible via menu. Professional/Colourful/Terminal themes have fixed color schemes; only the Original theme respects Light/Dark/Auto variants. The existing MainWindow-\*.axaml files will be **deleted** and replaced with reusable ResourceDictionary files.

**Key Decision:** Use ResourceDictionary merging for runtime theme switching instead of separate Window classes. This provides better UX (immediate theme application) and aligns with Avalonia best practices.

## Steps

### Phase 1: Data Model & Database ✅

1. **Create ThemePreference enum** ✅
   - Created [src/AStar.Dev.OneDrive.Sync.Client.Core/Models/Enums/ThemePreference.cs](src/AStar.Dev.OneDrive.Sync.Client.Core/Models/Enums/ThemePreference.cs)
   - Values: `OriginalAuto`, `OriginalLight`, `OriginalDark`, `Professional`, `Colourful`, `Terminal`

2. **Update data model** ✅
   - Updated [WindowPreferences.cs](src/AStar.Dev.OneDrive.Sync.Client.Core/Models/WindowPreferences.cs) with `Theme` property (default: `OriginalAuto`)
   - Updated [WindowPreferencesEntity.cs](src/AStar.Dev.OneDrive.Sync.Client.Core/Data/Entities/WindowPreferencesEntity.cs) with `string? Theme` column

3. **Create database migration** ✅
   - Migration `AddThemePreference` created successfully
   - Adds nullable `Theme` column to `WindowPreferences` table

4. **Update WindowPreferencesService** ✅
   - Modified [WindowPreferencesService.cs](src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/WindowPreferencesService.cs)
   - `LoadAsync()` maps string to enum with fallback to `OriginalAuto`
   - `SaveAsync()` converts enum to string for persistence

5. **Create unit tests** ✅
   - Created [WindowPreferencesServiceShould_ThemePreference.cs](test/AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/Services/WindowPreferencesServiceShould_ThemePreference.cs)
   - Tests for all 6 theme values, null handling, invalid value handling

### Phase 2: Extract Styles into ResourceDictionaries ✅

1. **Create Themes folder structure** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Themes/](src/AStar.Dev.OneDrive.Sync.Client/Themes/) directory
   - This will contain all theme ResourceDictionary files

2. **Extract Professional theme styles** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Themes/ProfessionalTheme.axaml](src/AStar.Dev.OneDrive.Sync.Client/Themes/ProfessionalTheme.axaml)
   - Extract and consolidate styles from:
     - [MainWindow-Professional.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Professional.axaml)
     - [AccountManagementViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewProfessional.axaml)
     - [SyncTreeViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewProfessional.axaml)
     - [SyncProgressViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewProfessional.axaml)
     - [ConflictResolutionViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewProfessional.axaml)
   - Define as `ResourceDictionary` with styled controls:
     - `Window` (Background: #F5F7FA)
     - `Border.card` (White background, 8px radius, subtle shadow)
     - `Button.primary` (Blue #0078D4, SemiBold)
     - `Button.secondary` (Transparent with blue border)
     - `TextBlock.heading` (18-22px, SemiBold)
     - `PathIcon` styles with blue foreground
   - Blue accent color (#0078D4), light gray backgrounds, subtle shadows (0 2 8 0 #10000000)

3. **Extract Colourful theme styles** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Themes/ColourfulTheme.axaml](src/AStar.Dev.OneDrive.Sync.Client/Themes/ColourfulTheme.axaml)
   - Extract and consolidate styles from:
     - [MainWindow-Colorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Colorful.axaml)
     - [AccountManagementViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewColorful.axaml)
     - [SyncTreeViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewColorful.axaml)
     - [SyncProgressViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewColorful.axaml)
     - [ConflictResolutionViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewColorful.axaml)
   - Define gradient backgrounds: Purple-violet (#667eea → #764ba2)
   - Glass morphism effects: Semi-transparent white (#E0FFFFFF)
   - Large border radius (16-25px), prominent shadows (0 8 32 0 #40000000)
   - Include emoji-based icons as TextBlock content (☁️, 👥, 📁, 💃)
   - Vibrant gradient buttons (red-yellow, pink-orange, cyan)

4. **Extract Terminal theme styles** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Themes/TerminalTheme.axaml](src/AStar.Dev.OneDrive.Sync.Client/Themes/TerminalTheme.axaml)
   - Extract and consolidate styles from:
     - [MainWindow-Terminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Terminal.axaml)
     - [AccountManagementViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewTerminal.axaml)
     - [SyncTreeViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewTerminal.axaml)
     - [SyncProgressViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewTerminal.axaml)
     - [ConflictResolutionViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewTerminal.axaml)
   - Green-on-black color scheme (#00FF41 on #0A0E0F, #111111)
   - Monospace fonts (Consolas, Courier New)
   - Neon glow effects for borders
   - ASCII art decorations as TextBlock content (█▓▒░, ◢, ◣)
   - Bracket-wrapped button text ([HISTORY], [LOGS])
   - Sharp corners (0px border radius)

5. **Create Original theme ResourceDictionaries (optional)** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Themes/OriginalLightTheme.axaml](src/AStar.Dev.OneDrive.Sync.Client/Themes/OriginalLightTheme.axaml)
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Themes/OriginalDarkTheme.axaml](src/AStar.Dev.OneDrive.Sync.Client/Themes/OriginalDarkTheme.axaml)
   - Extract any custom styles from current [MainWindow.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow.axaml) if present
   - For `OriginalAuto`, rely on Avalonia's built-in `FluentTheme` without custom ResourceDictionary

6. **Delete obsolete themed MainWindow files** ✅
   - Delete ALL Professional themed files:
     - [MainWindow-Professional.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Professional.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Professional.axaml.cs)
     - [AccountManagementViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewProfessional.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewProfessional.axaml.cs)
     - [SyncTreeViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewProfessional.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewProfessional.axaml.cs)
     - [SyncProgressViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewProfessional.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewProfessional.axaml.cs)
     - [ConflictResolutionViewProfessional.axaml](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewProfessional.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewProfessional.axaml.cs)
   - Delete ALL Colorful themed files:
     - [MainWindow-Colorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Colorful.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Colorful.axaml.cs)
     - [AccountManagementViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewColorful.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewColorful.axaml.cs)
     - [SyncTreeViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewColorful.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewColorful.axaml.cs)
     - [SyncProgressViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewColorful.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewColorful.axaml.cs)
     - [ConflictResolutionViewColorful.axaml](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewColorful.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewColorful.axaml.cs)
   - Delete ALL Terminal themed files:
     - [MainWindow-Terminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Terminal.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow-Terminal.axaml.cs)
     - [AccountManagementViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewTerminal.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Accounts/AccountManagementViewTerminal.axaml.cs)
     - [SyncTreeViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewTerminal.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncTreeViewTerminal.axaml.cs)
     - [SyncProgressViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewTerminal.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Syncronisation/SyncProgressViewTerminal.axaml.cs)
     - [ConflictResolutionViewTerminal.axaml](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewTerminal.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/SyncronisationConflicts/ConflictResolutionViewTerminal.axaml.cs)
   - **Total files to delete: ~30 files** (10 per theme × 3 themes)

### Phase 3: Theme Service for Runtime Switching ✅

1. **Create IThemeService interface** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/IThemeService.cs](src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/IThemeService.cs)
   - Method: `Task ApplyThemeAsync(ThemePreference theme, CancellationToken cancellationToken = default)`
   - Property: `ThemePreference CurrentTheme { get; }`
   - Event: `event EventHandler<ThemePreference>? ThemeChanged` - raised when theme changes

2. **Implement ThemeService** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/ThemeService.cs](src/AStar.Dev.OneDrive.Sync.Client.Infrastructure/Services/ThemeService.cs)
   - Add `[Service(ServiceLifetime.Singleton)]` attribute
   - Inject `IWindowPreferencesService` via constructor
   - Implement `ApplyThemeAsync()`:
     1. Clear all custom ResourceDictionaries from `Application.Current.Resources.MergedDictionaries` (preserve base FluentTheme)
     2. Based on `ThemePreference` parameter:
        - `OriginalLight`: Set `Application.Current.RequestedThemeVariant = ThemeVariant.Light`
        - `OriginalDark`: Set `Application.Current.RequestedThemeVariant = ThemeVariant.Dark`
        - `OriginalAuto`: Set `Application.Current.RequestedThemeVariant = ThemeVariant.Default`
        - `Professional`: Load and merge `ProfessionalTheme.axaml`, set variant to Default
        - `Colourful`: Load and merge `ColourfulTheme.axaml`, set variant to Default
        - `Terminal`: Load and merge `TerminalTheme.axaml`, set variant to Default
     3. Update `CurrentTheme` property
     4. Load current window preferences, update theme, save via `IWindowPreferencesService`
     5. Raise `ThemeChanged` event
   - Handle ResourceDictionary loading from embedded resources using `AvaloniaXamlLoader`

3. **Update App.axaml.cs for initial theme loading** ✅
   - Modify [App.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/App.axaml.cs) `OnFrameworkInitializationCompleted()`
   - Before creating `MainWindow`:
     1. Get `IThemeService` from DI container
     2. Load preferences via `IWindowPreferencesService.LoadAsync()`
     3. If preferences exist, apply theme: `await themeService.ApplyThemeAsync(preferences.Theme)`
     4. If no preferences, apply default: `await themeService.ApplyThemeAsync(ThemePreference.OriginalAuto)`
   - Handle async call in non-async method using `Task.Run().GetAwaiter().GetResult()` or make helper method
   - Keep single `MainWindow` instantiation (no more theme-specific window classes)

### Phase 4: Settings UI

1. **Create ThemePreferenceToDisplayNameConverter** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Converters/ThemePreferenceToDisplayNameConverter.cs](src/AStar.Dev.OneDrive.Sync.Client/Converters/ThemePreferenceToDisplayNameConverter.cs)
   - Implement `IValueConverter`
   - Map enum values to user-friendly display names:
     - `OriginalAuto` → "Original (Automatic)"
     - `OriginalLight` → "Original (Light)"
     - `OriginalDark` → "Original (Dark)"
     - `Professional` → "Professional"
     - `Colourful` → "Colourful"
     - `Terminal` → "Terminal / Hacker"

2. **Create SettingsWindow XAML** ✅
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Settings/SettingsWindow.axaml](src/AStar.Dev.OneDrive.Sync.Client/Settings/SettingsWindow.axaml) and [.axaml.cs](src/AStar.Dev.OneDrive.Sync.Client/Settings/SettingsWindow.axaml.cs)
   - Window properties: Width="600" Height="400", `SizeToContent="Manual"`, `CanResize="false"`, `WindowStartupLocation="CenterOwner"`
   - Add `ComboBox` for theme selection:
     - `ItemsSource` bound to all `ThemePreference` enum values (use `ObjectDataProvider` or ViewModel property)
     - `SelectedItem` bound to `SettingsViewModel.SelectedTheme`
     - Use `ThemePreferenceToDisplayNameConverter` for `DisplayMemberPath` or `ItemTemplate`
   - Add preview area showing sample UI elements in selected theme (optional, nice-to-have)
   - Add buttons:
     - "Apply" button bound to `SettingsViewModel.ApplyThemeCommand`
     - "Close" button bound to `SettingsViewModel.CloseCommand`
   - **Remove** "Theme changes will take effect after restarting" message (no longer needed)
   - Add "Theme applied successfully" status message (shown after Apply)

3. **Create SettingsViewModel**
   - Create [src/AStar.Dev.OneDrive.Sync.Client/Settings/SettingsViewModel.cs](src/AStar.Dev.OneDrive.Sync.Client/Settings/SettingsViewModel.cs)
   - Inherit from `ReactiveObject`
   - Inject `IThemeService` via constructor
   - Properties:
     - `ThemePreference SelectedTheme` - initialized from `_themeService.CurrentTheme`
     - `string? StatusMessage` - for "Theme applied" feedback
     - `IEnumerable<ThemePreference> AvailableThemes` - all enum values for ComboBox binding
   - Commands:
     - `ReactiveCommand ApplyThemeCommand`:
       - Implementation: `await _themeService.ApplyThemeAsync(SelectedTheme)`
       - Set `StatusMessage = "Theme applied successfully"`
       - Theme changes immediately (no restart)
     - `ReactiveCommand CloseCommand`:
       - Implementation: Close the window (cast `DataContext` owner or use window reference)
   - Subscribe to `_themeService.ThemeChanged` event to update `SelectedTheme` if theme changes externally

4. **Update MainWindow menu**
   - Modify [MainWindow.axaml](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindow.axaml) File menu
   - Add `<MenuItem Header="_Settings..." Command="{Binding OpenSettingsCommand}">` between "View Debug Logs" and the `<Separator/>`
   - Use underscore for keyboard shortcut (Alt+S)

5. **Update MainWindowViewModel**
   - Modify [MainWindowViewModel.cs](src/AStar.Dev.OneDrive.Sync.Client/MainWindow/MainWindowViewModel.cs)
   - Add `ICommand OpenSettingsCommand` property
   - Command implementation:
     - Retrieve `SettingsViewModel` from DI container: `var settingsVm = App.Host.Services.GetRequiredService<SettingsViewModel>()`
     - Create `SettingsWindow` instance
     - Set `settingsWindow.DataContext = settingsVm`
     - Call `await settingsWindow.ShowDialog(this._window)` (pass parent window for modal centering)
   - Mark `SettingsViewModel` with `[Service]` attribute in its class definition for DI registration

> ### Phase 5: Testing ✅

1. **Fix existing WindowPreferencesService tests** ✅

    - Debug and fix failing tests in [WindowPreferencesServiceShould_ThemePreference.cs](test/AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/Services/WindowPreferencesServiceShould_ThemePreference.cs)
      - ✅ All 9 tests passing for theme persistence across all 6 enum values

2. **Create ThemeService unit tests** ✅ - Create [test/AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/Services/ThemeServiceShould.cs](test/AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit/Services/ThemeServiceShould.cs)

    - Tests:
      - `ApplyTheme_UpdatesCurrentThemeProperty()`
      - `ApplyTheme_PersistsToWindowPreferences()` - verify `IWindowPreferencesService.SaveAsync()` called
      - `ApplyTheme_RaisesThemeChangedEvent()` - verify event fired with correct theme
      - `ApplyThemeOriginalLight_SetsRequestedThemeVariantToLight()`
      - `ApplyThemeOriginalDark_SetsRequestedThemeVariantToDark()`
      - `ApplyThemeOriginalAuto_SetsRequestedThemeVariantToDefault()`
      - `ApplyThemeProfessional_LoadsProfessionalResourceDictionary()` - verify merged dictionaries contain Professional theme
      - `ApplyThemeColourful_LoadsColourfulResourceDictionary()`
      - `ApplyThemeTerminal_LoadsTerminalResourceDictionary()`
    - Mock `IWindowPreferencesService` using NSubstitute or test double
    - Mock `Application.Current` for testing (may need to use integration test or skip this verification)      - ✅ All 12 tests passing (covers all required scenarios plus additional cases)

3. **Create SettingsViewModel tests** ✅

    - Create [test/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Settings/SettingsViewModelShould.cs](test/AStar.Dev.OneDrive.Sync.Client.Tests.Unit/Settings/SettingsViewModelShould.cs)
    - Tests:
      - `ApplyThemeCommand_CallsThemeServiceApplyThemeAsync()`
      - `ApplyThemeCommand_SetsStatusMessage()`
      - `SelectedTheme_InitializesFromThemeServiceCurrentTheme()`
      - `SelectedTheme_UpdatesWhenThemeChangedEventFires()` - verify reactive subscription works
      - `AvailableThemes_ContainsAllSixThemePreferences()`
    - Mock `IThemeService` using interface and verify method calls      - ✅ All 7 tests passing (covers all required scenarios plus additional cases)

4. **Create integration tests for theme switching**

    - Create [test/AStar.Dev.OneDrive.Sync.Client.Tests.Integration/ThemeSwitchingShould.cs](test/AStar.Dev.OneDrive.Sync.Client.Tests.Integration/ThemeSwitchingShould.cs)
    - Test full workflow: Load preferences → Apply theme → Verify UI updated → Save preferences → Reload app → Verify persistence
    - May require Avalonia headless testing or manual verification

5. **Manual testing checklist**- ✅ All 10 tests passing

    - Launch app → Verify default theme (OriginalAuto) applies automatically
    - File → Settings → Select "Professional" → Click Apply → **Verify immediate theme change** (no restart)
    - Verify window background, button styles, colors update instantly
    - Change to "Colourful" → Apply → Verify gradients and glass effects appear immediately
    - Change to "Terminal" → Apply → Verify green-on-black theme with monospace fonts
    - Close app, relaunch → Verify last selected theme persists
    - Test all 6 themes for visual correctness against original designs
    - Verify window position/size still persists correctly
    - Test theme switching while other windows are open (dialogs, progress views)
    - Verify no console errors or exceptions during theme switching

### Phase 6: Documentation

1. **Update user documentation**
   - Update [docs/user-manual.md](docs/user-manual.md) - add "Changing Themes" section
     - Document step-by-step:
       1. File menu → Settings...
       2. Select desired theme from dropdown
       3. Theme changes **immediately** (no restart required)
   - Include screenshots of each theme
   - Describe theme characteristics:
     - **Original (Automatic)**: Follows system light/dark preference, clean Fluent design
     - **Original (Light/Dark)**: Manual light or dark mode
     - **Professional**: Corporate minimalist with blue accents, subtle shadows, ample whitespace
     - **Colourful**: Playful gradients, glass morphism, vibrant colors, emoji icons
     - **Terminal**: Retro hacker aesthetic, green-on-black, monospace fonts, ASCII art

2. **Update design documentation**
   - Update [docs/design/options-for-ui-changes.md](docs/design/options-for-ui-changes.md)
   - Add "Implementation Status" section noting ResourceDictionary approach chosen
   - Document architecture: Single MainWindow + swappable ResourceDictionaries
   - Update [docs/design/QUICK-START-COLORFUL.md](docs/design/QUICK-START-COLORFUL.md)
   - Add deprecation notice: Manual `App.axaml.cs` switching replaced by Settings UI
   - Document new approach: File → Settings → Theme selection

3. **Create ADR for theme system architecture**
   - Create [docs/ADRs/006-theme-system-architecture.md](docs/ADRs/006-theme-system-architecture.md) (or next available number)
   - Document decision context: User request for runtime theme switching without restart
   - Decision: ResourceDictionary merging over separate Window classes
   - Rationale:
     - **Better UX**: Immediate theme application, no interruption
     - **Maintainability**: Single MainWindow, reduced code duplication
     - **Avalonia best practices**: Aligns with recommended styling approach
     - **Extensibility**: Easy to add new themes (just add new ResourceDictionary)
   - Consequences:
     - **Positive**: Better user experience, cleaner architecture, easier testing
     - **Negative**: Required refactoring ~30 existing themed XAML files into ResourceDictionaries
     - **Neutral**: Slightly more complex XAML resource management
   - Alternatives considered: Separate Window classes (rejected due to restart requirement)

## Verification

- **Build**: `dotnet build` - no errors/warnings
- **Migration**: `dotnet ef database update --project src/AStar.Dev.OneDrive.Sync.Client.Infrastructure --startup-project src/AStar.Dev.OneDrive.Sync.Client` - migration applies successfully
- **Tests**: `dotnet test` - all tests pass (including WindowPreferencesService theme tests, ThemeService tests, SettingsViewModel tests)
- **Manual testing workflow**:
  1. Fresh launch → Default OriginalAuto theme applied automatically
  2. File → Settings → Select "Professional" → Apply → **Immediate visual change** (no restart prompt)
  3. Verify Professional styles: Blue accents, subtle shadows, card layouts
  4. Select "Colourful" → Apply → Verify gradients, glass effects, emoji icons appear instantly
  5. Select "Terminal" → Apply → Verify green-on-black, monospace fonts, ASCII art
  6. Close app completely
  7. Relaunch → Verify Terminal theme persists (loaded from database)
  8. Test Original (Light/Dark/Auto) variants with OS theme switching
  9. Open account management dialog → Verify theme applies to dialog
  10. Open sync progress view → Verify theme applies
  11. Test theme switching while dialogs are open → Verify UI updates dynamically
  12. Verify window position/size persistence unaffected by theme changes

## Decisions

- **ResourceDictionaries over separate Window classes**: Enables runtime theme switching without restart, better UX, aligns with Avalonia patterns, reduces code duplication
- **Single enum with 6 values**: Simpler data model than separate `UiStyle` + `ThemeVariant` properties, prevents invalid combinations (e.g., "Terminal + Light Mode")
- **No restart required**: Immediate theme application via ResourceDictionary merging, vastly improved user experience
- **Enum-to-string persistence**: Store as string in database for readability, easier debugging, migration safety, supports future theme additions
- **Default theme OriginalAuto**: Matches current behavior (follows OS preference), best UX for new users
- **Singleton ThemeService**: Single source of truth for current theme state, manages theme lifecycle, raises events for reactive updates
- **Delete ~30 themed files**: Consolidate into 3 ResourceDictionary files (Professional, Colourful, Terminal), reduces maintenance burden
- **Apply button vs auto-apply**: Explicit Apply button gives users control to preview before committing, prevents accidental theme spam
