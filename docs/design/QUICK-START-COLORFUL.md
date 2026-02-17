# Quick Start: Testing the Colorful Design

> **DEPRECATION NOTICE**
>
> **This manual approach is deprecated as of February 2026.**
>
> The application now includes a Settings UI for runtime theme switching. Manual
> editing of `App.axaml.cs` is no longer necessary.
>
> **New Approach:**
>
> 1. Launch the application
> 2. Click **File** -> **Settings...**
> 3. Select desired theme from dropdown
> 4. Theme changes immediately (no restart required)
>
> **Available Themes:** Original (Auto/Light/Dark), Professional, Colourful, Terminal
>
> See [User Manual - Changing Themes](../user-manual.md#changing-themes) for detailed instructions.
>
> ---

## Fastest Way to See the Colorful Design

1. Open [App.axaml.cs](../../src/AStar.Dev.OneDrive.Sync.Client/App.axaml.cs)

2. Find this line (around line 28):

   ```csharp
   desktop.MainWindow = new MainWindow.MainWindow();
   ```

3. Change it to:

   ```csharp
   desktop.MainWindow = new MainWindow.MainWindowColorful();
   ```

4. Build and run:

```bash
   dotnet build
   dotnet run --project src/AStar.Dev.OneDrive.Sync.Client
```

## That's it! 🎉

The colorful, playful design should now appear with:

- Purple gradient background
- Glassmorphism cards
- Vibrant rainbow buttons
- Emoji icons everywhere
- Fun, informal messaging

## To Switch Back

Just change the line back to:

```csharp
desktop.MainWindow = new MainWindow.MainWindow();
```

## Files Created

All colorful theme files are named with "Colorful" suffix:

- `MainWindow-Colorful.axaml`
- `AccountManagementViewColorful.axaml`
- `SyncTreeViewColorful.axaml`
- `ConflictResolutionViewColorful.axaml`
- `SyncProgressViewColorful.axaml`

Plus 3 new converters in the `Converters/` folder.
