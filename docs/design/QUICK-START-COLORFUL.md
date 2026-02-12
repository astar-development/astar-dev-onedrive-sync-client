# Quick Start: Testing the Colorful Design

## Fastest Way to See the Colorful Design

1. Open [App.axaml.cs](../../src/AStar.Dev.OneDrive.Client/App.axaml.cs)

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
   dotnet run --project src/AStar.Dev.OneDrive.Client
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
