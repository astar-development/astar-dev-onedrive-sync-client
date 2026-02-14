# Using the Colorful Design Theme

## Overview

The colorful, playful design theme has been implemented with the following files:

### Main Window

- `MainWindow-Colorful.axaml` / `MainWindow-Colorful.axaml.cs`

### View Components

- `AccountManagementViewColorful.axaml` / `AccountManagementViewColorful.axaml.cs`
- `SyncTreeViewColorful.axaml` / `SyncTreeViewColorful.axaml.cs`
- `ConflictResolutionViewColorful.axaml` / `ConflictResolutionViewColorful.axaml.cs`
- `SyncProgressViewColorful.axaml` / `SyncProgressViewColorful.axaml.cs`

### Converters

- `InitialsConverter.cs` - Converts display names to initials (e.g., "John Doe" → "JD")
- `BoolToStatusColorConverter.cs` - Converts boolean to vibrant status colors
- `BoolToStatusTextConverter.cs` - Converts boolean to friendly status text

## How to Use

### Option 1: Switch the Main Window (Recommended)

Edit [App.axaml.cs](../../src/AStar.Dev.OneDrive.Sync.Client/App.axaml.cs):

```csharp
// Change this line:
desktop.MainWindow = new MainWindow.MainWindow();

// To this:
desktop.MainWindow = new MainWindow.MainWindowColorful();
```

### Option 2: Create a Configuration Setting

Add to `appsettings.json`:

```json
{
  "UI": {
    "Theme": "Colorful" // Options: "Default", "Colorful", "Professional", "Terminal"
  }
}
```

Then update `App.axaml.cs`:

```csharp
var theme = configuration["UI:Theme"] ?? "Default";

desktop.MainWindow = theme switch
{
    "Colorful" => new MainWindow.MainWindowColorful(),
    "Professional" => new MainWindow.MainWindowProfessional(), // Not yet implemented
    "Terminal" => new MainWindow.MainWindowTerminal(),         // Not yet implemented
    _ => new MainWindow.MainWindow()
};
```

## Design Features

### Visual Identity

- **Colors**: Vibrant gradients (purple/blue, red/yellow, cyan/turquoise)
- **Typography**: Bold, playful fonts with emojis
- **Shapes**: Rounded corners (16-25px border radius)
- **Effects**: Glassmorphism, soft shadows, gradient overlays

### Key Visual Elements

#### Gradient Background

The main window uses a purple-to-violet gradient background that creates a modern, energetic feel.

#### Glass Cards

Content panels use a semi-transparent white background with blur effects, creating a "frosted glass" appearance.

#### Colorful Buttons

- **Primary Actions**: Red-to-yellow gradient
- **Add Account**: Pink-to-orange gradient
- **Sign In**: Cyan gradient
- **Sign Out**: Turquoise gradient

#### Emoji Usage

Icons are replaced with emojis for a friendly, informal feel:

- ☁️ Cloud/OneDrive
- 👥 Accounts
- 📁 Folders
- ⚠️ Conflicts
- 🚀 Sync Progress
- ✅ Success
- ❌ Error/Cancel

#### Status Indicators

- **Connected**: Cyan (#4ECDC4) with ✓
- **Disconnected**: Red (#FF6B6B) with ✗

## Technical Notes

### Missing Properties

Some properties referenced in the XAML may need to be added to ViewModels:

- `Email` property on account model (currently shows `DisplayName` only)
- `HasUnresolvedConflicts` on MainWindowViewModel (for conflict button visibility)

### Converter Dependencies

All required converters are included:

- `InitialsConverter` - extracts initials from names
- `BoolToStatusColorConverter` - boolean to color
- `BoolToStatusTextConverter` - boolean to text
- `EnumToBooleanConverter` - already exists for conflict resolution
- `BooleanNegationConverter` - already exists

### Animation Considerations

The rotating loading emoji uses RenderTransform animations. Ensure your Avalonia version supports these animations.

## Preview

The colorful design offers:

- High-energy, approachable aesthetics
- Clear visual hierarchy through color
- Friendly, informal tone
- Modern glassmorphism effects
- Gradients throughout the UI
- Emoji-based iconography

## Customization

To adjust colors, modify the gradient stops in:

- Window background: `MainWindow-Colorful.axaml`
- Button gradients: Individual button definitions
- Card backgrounds: Border.Background properties

Example:

```xml
<LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
    <GradientStop Color="#YourStartColor" Offset="0"/>
    <GradientStop Color="#YourEndColor" Offset="1"/>
</LinearGradientBrush>
```

## Known Limitations

1. **Account Model**: May need `Email` property added
2. **ViewModel Properties**: Some bindings assume properties that may not exist yet
3. **Performance**: Heavy use of gradients and effects may impact performance on older hardware
4. **Accessibility**: Emoji icons may not work well with screen readers

## Next Steps

To make other themes work:

1. Implement Professional theme (corporate minimalist)
2. Implement Terminal theme (retro hacker aesthetic)
3. Add theme switcher in UI settings
4. Persist user theme preference
