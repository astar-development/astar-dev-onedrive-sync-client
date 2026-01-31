# Multi-Account OneDrive Sync - UX Implementation Plan

**Project**: AStar Dev - OneDrive Client V3  
**Date**: January 5, 2026  
**Status**: Planning Phase

---

## Executive Summary

This document outlines the implementation plan for rewriting the OneDrive sync solution with improved UX and multi-account support. The key improvements include support for up to 3 OneDrive accounts with separate sync directories, an intuitive account management interface, and a hierarchical folder selection tree.

---

## Requirements Overview

### Core Features
1. **Multi-Account Support**: Support up to 3 OneDrive accounts simultaneously
2. **Per-Account Sync Directories**: Each account syncs to a separate local directory
3. **Dynamic Account Management UI**: Login/logout buttons that adapt based on number of active accounts
4. **Hierarchical Folder Selection**: Tree view showing OneDrive folder structure with checkboxes
5. **Default Selection**: All folders checked by default for sync

### User Flow
1. Initial screen shows single Login/Logout button pair
2. After first login, "Add Account" button appears
3. Subsequent accounts show account-specific Login/Logout buttons
4. Post-login, "Show Sync Tree" button becomes available
5. Sync tree displays OneDrive root folders (Documents, Pictures, Videos, etc.)
6. Users can expand nodes and check/uncheck folders for sync

---

## Architecture & Design

### Project Structure

```
src/
├── Authentication/
│   ├── IAuthService.cs
│   ├── AuthService.cs                    // Handles MSAL authentication
│   ├── IMultiAccountManager.cs
│   └── MultiAccountManager.cs            // NEW: Manages multiple auth sessions
├── Views/
│   ├── AccountManagementView.axaml       // NEW: Account login/logout UI
│   ├── SyncTreeView.axaml                // NEW: Hierarchical folder tree
│   └── MainWindow.axaml                  // Main entry point
├── ViewModels/
│   ├── AccountManagementViewModel.cs     // NEW: Account UI logic
│   ├── SyncTreeViewModel.cs              // NEW: Tree selection logic
│   └── MainWindowViewModel.cs            // Main coordinator
├── Models/
│   ├── AccountInfo.cs                    // NEW: Account metadata
│   ├── OneDriveFolderNode.cs             // NEW: Tree node model
│   └── SyncConfiguration.cs              // NEW: Per-account sync config
├── Services/
│   ├── OneDriveServices/
│   │   ├── IOneDriveApiService.cs
│   │   ├── OneDriveApiService.cs         // Graph API interactions
│   │   ├── IFolderTreeService.cs         // NEW: Fetch folder hierarchy
│   │   ├── FolderTreeService.cs          // NEW: Implementation
│   │   ├── IDeltaSyncEngine.cs           // NEW: Two-way delta sync
│   │   ├── DeltaSyncEngine.cs            // NEW: Download + Upload coordination
│   │   ├── IFileWatcherService.cs        // NEW: Detect local file changes
│   │   └── FileWatcherService.cs         // NEW: FileSystemWatcher wrapper
│   ├── ConflictResolution/
│   │   ├── IConflictResolver.cs          // NEW: Handle sync conflicts
│   │   └── ConflictResolver.cs           // NEW: Strategy-based resolution
│   └── SettingsAndPreferences/
│       ├── IAccountRepository.cs         // NEW: Database access
│       └── AccountRepository.cs          // NEW: EF Core implementation
└── Common/
    └── Constants.cs                      // Max accounts, default paths, etc.
```

---

## Implementation Phases

### Phase 1: Foundation & Multi-Account Management

**Goal**: Establish multi-account infrastructure and persistence

#### 1.1 Models & Configuration

**Models to Create**:

```csharp
/// <summary>
/// Represents user preferences for the application window.
/// </summary>
public sealed record WindowPreferences
{
    public double? X { get; init; }                      // Window X position (null = center on screen)
    public double? Y { get; init; }                      // Window Y position
    public double Width { get; init; } = 800;            // Window width
    public double Height { get; init; } = 600;           // Window height
    public bool IsMaximized { get; init; }               // Whether window is maximized
}

/// <summary>
/// Represents a configured OneDrive account.
/// </summary>
public sealed record AccountInfo
{
    public required string AccountId { get; init; }      // Unique identifier (MSAL HomeAccountId)
    public required string DisplayName { get; init; }    // User-friendly name (email)
    public required string LocalSyncPath { get; init; }  // User-selected local directory for this account
    public bool IsAuthenticated { get; init; }           // Current auth status
    public DateTimeOffset? LastSyncUtc { get; init; }
    public string? DeltaToken { get; init; }             // Graph API delta token for resuming sync
}

/// <summary>
/// Sync configuration for a single account (stored in database).
/// </summary>
public sealed record SyncConfiguration
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<string> SelectedFolderIds { get; init; }  // OneDrive folder IDs to sync
    public required IReadOnlyDictionary<string, bool> FolderSelections { get; init; } // Path -> IsSelected
    public DateTimeOffset LastModifiedUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the current sync state for tracking progress.
/// </summary>
public sealed record SyncState
{
    public required string AccountId { get; init; }
    public SyncStatus Status { get; init; }              // Running, Paused, Completed, Failed
    public int TotalFiles { get; init; }
    public int CompletedFiles { get; init; }
    public long TotalBytes { get; init; }
    public long CompletedBytes { get; init; }
    public int FilesDownloading { get; init; }           // Current download count
    public int FilesUploading { get; init; }             // Current upload count
    public int ConflictsDetected { get; init; }          // Files with conflicts
    public double MegabytesPerSecond { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    public DateTimeOffset? LastUpdateUtc { get; init; }
}

public enum SyncStatus
{
    Idle,
    Running,
    Paused,
    Completed,
    Failed
}

/// <summary>
/// Represents a file sync conflict.
/// </summary>
public sealed record SyncConflict
{
    public required string FileId { get; init; }
    public required string FilePath { get; init; }
    public DateTimeOffset LocalModifiedUtc { get; init; }
    public DateTimeOffset RemoteModifiedUtc { get; init; }
    public long LocalSize { get; init; }
    public long RemoteSize { get; init; }
    public ConflictResolutionStrategy ResolvedStrategy { get; init; }
}

public enum ConflictResolutionStrategy
{
    Unresolved,        // User hasn't chosen yet
    KeepLocal,         // Upload local version (overwrite remote)
    KeepRemote,        // Download remote version (overwrite local)
    KeepBoth,          // Rename local file, download remote
    Skip               // Don't sync this file
}

public enum SyncDirection
{
    Download,          // From OneDrive to local
    Upload,            // From local to OneDrive
    Both               // Bidirectional (for conflict scenarios)
}
```

**Implementation Notes**:
- Use **SQLite database** for all persistent storage (settings, sync state, delta tokens)
- Database location: `AppData\Local\AStarOneDriveClient\sync.db`
- Use **Entity Framework Core** with SQLite provider for data access
- Add `IAccountConfigRepository` and `ISyncStateRepository` interfaces for testability
- All records are immutable - use `with` expressions for updates

#### 1.2 Multi-Account Authentication Manager

**New Service**: `IMultiAccountManager`

```csharp
public interface IMultiAccountManager
{
    /// <summary>
    /// Gets all configured accounts.
    /// </summary>
    IReadOnlyList<AccountInfo> GetAccounts();
    
    /// <summary>
    /// Adds a new account (max 3) with user-selected sync directory.
    /// </summary>
    /// <param name="localSyncPath">User-selected local directory (must be unique across accounts)</param>
    Task<AccountInfo> AddAccountAsync(string localSyncPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes an account and optionally deletes synced data.
    /// </summary>
    /// <param name="deleteLocalData">If true, deletes all synced files in LocalSyncPath</param>
    Task RemoveAccountAsync(string accountId, bool deleteLocalData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Authenticates a specific account.
    /// </summary>
    Task<bool> LoginAsync(string accountId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logs out a specific account.
    /// </summary>
    Task LogoutAsync(string accountId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a sync path is unique and valid.
    /// </summary>
    bool ValidateSyncPath(string path, out string? errorMessage);
    
    /// <summary>
    /// Observable stream of account changes.
    /// </summary>
    IObservable<IReadOnlyList<AccountInfo>> AccountsChanged { get; }
}
```

**Key Responsibilities**:
- Coordinate with existing `IAuthService` for MSAL operations
- Manage MSAL token cache per account (using persistent cache serialization to disk)
- Enforce 3-account limit
- Persist account metadata via database repository
- Validate user-selected sync paths (uniqueness, writeability, not nested)
- Handle account removal with optional data deletion (prompts user)
- Ensure MSAL tokens are cached to disk to avoid repeated login prompts

**Testing Priority**: HIGH (business logic)

---

### Phase 2: Account Management UI

**Goal**: Build dynamic account management interface

#### 2.1 AccountManagementView (AXAML)

**UI Components**:
- `ItemsControl` bound to `Accounts` collection
- Each item contains:
  - `TextBlock` showing account email/name
  - `Button` for Login (visible when `!IsAuthenticated`)
  - `Button` for Logout (visible when `IsAuthenticated`)
  - `Button` for Remove account
  - `Button` for "Show Sync Tree" (visible when `IsAuthenticated`)
- `Button` for "Add Account" (visible when `Accounts.Count < 3`)

**Layout Considerations**:
- Stack accounts vertically
- Use `IsVisible` binding for conditional button display
- Consider `DataTemplate` for account items
- Disable "Add Account" when limit reached

#### 2.2 AccountManagementViewModel

```csharp
public sealed class AccountManagementViewModel : ReactiveObject
{
    private readonly IMultiAccountManager _accountManager;
    private readonly CompositeDisposable _disposables = new();
    
    // Observable collection for UI binding
    public ObservableCollection<AccountViewModel> Accounts { get; } = [];
    
    // Commands
    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }
    public ReactiveCommand<string, Unit> LoginCommand { get; }
    public ReactiveCommand<string, Unit> LogoutCommand { get; }
    public ReactiveCommand<string, Unit> RemoveAccountCommand { get; }
    public ReactiveCommand<string, Unit> ShowSyncTreeCommand { get; }
    
    // Properties
    public bool CanAddAccount => Accounts.Count < AppSettings.MaxAccounts;
    
    public AccountManagementViewModel(IMultiAccountManager accountManager)
    {
        _accountManager = accountManager;
        
        // Subscribe to account changes
        _accountManager.AccountsChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateAccounts)
            .DisposeWith(_disposables);
        
        // Setup commands with proper error handling
        AddAccountCommand = ReactiveCommand.CreateFromTask(AddAccountAsync);
        LoginCommand = ReactiveCommand.CreateFromTask<string>(LoginAsync);
        LogoutCommand = ReactiveCommand.CreateFromTask<string>(LogoutAsync);
        RemoveAccountCommand = ReactiveCommand.CreateFromTask<string>(RemoveAccountAsync);
        ShowSyncTreeCommand = ReactiveCommand.Create<string>(ShowSyncTree);
    }
    
    private async Task AddAccountAsync(CancellationToken cancellationToken)
    {
        if (Accounts.Count >= AppSettings.MaxAccounts)
            return;
        
        // Show folder picker dialog for user to select sync directory
        var folderDialog = new OpenFolderDialog
        {
            Title = "Select Sync Directory for New Account"
        };
        
        var selectedPath = await folderDialog.ShowAsync(GetMainWindow());
        if (string.IsNullOrEmpty(selectedPath))
            return; // User cancelled
        
        // Validate path
        if (!_accountManager.ValidateSyncPath(selectedPath, out var errorMessage))
        {
            await ShowErrorDialog(errorMessage!);
            return;
        }
        
        await _accountManager.AddAccountAsync(selectedPath, cancellationToken);
        // Auto-trigger login after adding
    }
    
    private async Task RemoveAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        var account = Accounts.FirstOrDefault(a => a.AccountId == accountId);
        if (account is null)
            return;
        
        // Prompt user: Keep or delete synced data?
        var result = await ShowDeleteDataDialog(
            $"Remove account {account.DisplayName}?",
            $"Do you want to delete synced files in {account.LocalSyncPath}?");
        
        await _accountManager.RemoveAccountAsync(accountId, result.DeleteData, cancellationToken);
    }
    
    private void UpdateAccounts(IReadOnlyList<AccountInfo> accounts)
    {
        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(new AccountViewModel(account));
        }
        this.RaisePropertyChanged(nameof(CanAddAccount));
    }
}

/// <summary>
/// ViewModel for individual account in the list.
/// </summary>
public sealed class AccountViewModel : ReactiveObject
{
    private bool _isAuthenticated;
    
    public string AccountId { get; }
    public string DisplayName { get; }
    public string LocalSyncPath { get; }
    
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
    }
    
    // UI visibility helpers
    public bool ShowLoginButton => !IsAuthenticated;
    public bool ShowLogoutButton => IsAuthenticated;
    public bool ShowSyncTreeButton => IsAuthenticated;
    
    public AccountViewModel(AccountInfo account)
    {
        AccountId = account.AccountId;
        DisplayName = account.DisplayName;
        LocalSyncPath = account.LocalSyncPath;
        IsAuthenticated = account.IsAuthenticated;
    }
}
```

**Testing Priority**: HIGH (property notifications, command execution)

**Specific Tests**:
- Verify `CanAddAccount` updates when accounts added/removed
- Test `ShowLoginButton` / `ShowLogoutButton` visibility logic
- Verify commands fire and propagate to `IMultiAccountManager`
- Test `AccountsChanged` observable updates `Accounts` collection

---

### Phase 3: OneDrive Folder Tree Service

**Goal**: Fetch and model OneDrive folder hierarchy

#### 3.1 Folder Tree Models

```csharp
/// <summary>
/// Represents a node in the OneDrive folder tree with tri-state checkbox support.
/// </summary>
public sealed class OneDriveFolderNode : ReactiveObject
{
    public required string Id { get; init; }              // OneDrive folder ID
    public required string Name { get; init; }
    public required string Path { get; init; }            // Full path (e.g., /Documents/Projects)
    
    private bool? _isSelected = true;                     // Default checked (tri-state: true/false/null)
    /// <summary>
    /// Gets or sets the selection state.
    /// - true: All children selected (or leaf node selected)
    /// - false: All children unselected (or leaf node unselected)
    /// - null: Some children selected (indeterminate state)
    /// </summary>
    public bool? IsSelected
    {
        get
        {
            // If no children (leaf node), return explicit selection state
            if (!HasChildren)
                return _isSelected;
            
            // For parent nodes, compute tri-state based on children
            var childStates = Children.Select(c => c.IsSelected).Distinct().ToList();
            
            if (childStates.Count == 1)
            {
                // All children have same state
                return childStates[0];
            }
            
            // Mixed states or contains null (indeterminate)
            return null;
        }
        set
        {
            if (_isSelected == value)
                return;
            
            _isSelected = value;
            this.RaisePropertyChanged(nameof(IsSelected));
            
            // Cascade to children when explicitly set (not computed)
            if (value.HasValue && HasChildren)
            {
                foreach (var child in Children)
                {
                    child.IsSelected = value;
                }
            }
            
            SelectionChanged?.Invoke();  // Notify parent ViewModel for auto-save
        }
    }
    
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
    
    public ObservableCollection<OneDriveFolderNode> Children { get; } = [];
    public bool HasChildren => Children.Count > 0;
    
    // Event for notifying ViewModel of selection changes
    public Action? SelectionChanged { get; set; }
    
    /// <summary>
    /// Updates parent node's tri-state after child selection changes.
    /// </summary>
    public void NotifyChildSelectionChanged()
    {
        this.RaisePropertyChanged(nameof(IsSelected));
        SelectionChanged?.Invoke();
    }
}

/// <summary>
/// Represents the root special folders in OneDrive.
/// </summary>
public static class OneDriveSpecialFolders
{
    public const string Documents = "Documents";
    public const string Pictures = "Pictures";
    public const string Videos = "Videos";
    public const string Music = "Music";
    public const string Desktop = "Desktop";
    
    public static readonly string[] All = 
    [
        Documents, Pictures, Videos, Music, Desktop
    ];
}
```

#### 3.2 FolderTreeService

```csharp
public interface IFolderTreeService
{
    /// <summary>
    /// Gets the root-level special folders for an account.
    /// </summary>
    Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(
        string accountId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets child folders for a specific folder.
    /// </summary>
    Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(
        string accountId, 
        string parentFolderId, 
        CancellationToken cancellationToken = default);
}

public sealed class FolderTreeService : IFolderTreeService
{
    private readonly IOneDriveApiService _apiService;
    private readonly ILogger<FolderTreeService> _logger;
    
    public FolderTreeService(
        IOneDriveApiService apiService,
        ILogger<FolderTreeService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }
    
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(
        string accountId, 
        CancellationToken cancellationToken = default)
    {
        // Use Microsoft Graph API:
        // GET /me/drive/special/{specialFolder}
        // GET /me/drive/root/children?$filter=folder ne null
        
        var rootFolders = new List<OneDriveFolderNode>();
        
        foreach (var specialFolder in OneDriveSpecialFolders.All)
        {
            try
            {
                var driveItem = await _apiService.GetSpecialFolderAsync(
                    accountId, 
                    specialFolder, 
                    cancellationToken);
                
                rootFolders.Add(new OneDriveFolderNode
                {
                    Id = driveItem.Id,
                    Name = specialFolder,
                    Path = $"/{specialFolder}",
                    IsSelected = true  // Default checked
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to fetch special folder {FolderName} for account {AccountId}", 
                    specialFolder, accountId);
            }
        }
        
        return rootFolders;
    }
    
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(
        string accountId, 
        string parentFolderId, 
        CancellationToken cancellationToken = default)
    {
        // GET /me/drive/items/{parentFolderId}/children?$filter=folder ne null
        
        var children = await _apiService.GetFolderChildrenAsync(
            accountId, 
            parentFolderId, 
            cancellationToken);
        
        return children
            .Where(item => item.Folder is not null)  // Only folders
            .Select(item => new OneDriveFolderNode
            {
                Id = item.Id,
                Name = item.Name,
                Path = $"{item.ParentReference.Path}/{item.Name}",
                IsSelected = true  // Default checked
            })
            .ToList();
    }
}
```

**Microsoft Graph API Notes**:
- Special folders endpoint: `/me/drive/special/{folderName}`
- Children endpoint: `/me/drive/items/{itemId}/children`
- Use `$filter=folder ne null` to get only folders
- Use `$select=id,name,folder,parentReference` to minimize payload

**Testing Priority**: MEDIUM (integration with Graph API)

---

### Phase 4: Sync Tree UI

**Goal**: Interactive hierarchical folder tree with checkboxes

#### 4.1 SyncTreeView (AXAML)

**UI Components**:
- `TreeView` control bound to `RootFolders` collection
- Each `TreeViewItem` contains:
  - `CheckBox` bound to `IsSelected`
  - `TextBlock` showing folder name
  - Lazy-load children on expand
- "Save Configuration" button
- "Start Sync" button

**Avalonia TreeView Pattern**:
```xml
<TreeView ItemsSource="{Binding RootFolders}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal">
                <CheckBox IsChecked="{Binding IsSelected}" />
                <TextBlock Text="{Binding Name}" Margin="5,0,0,0" />
            </StackPanel>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

**Lazy Loading**:
- Listen to `TreeViewItem.Expanded` event
- Load children on first expansion
- Cache loaded children

#### 4.2 SyncTreeViewModel

```csharp
public sealed class SyncTreeViewModel : ReactiveObject
{
    private readonly IFolderTreeService _folderTreeService;
    private readonly IAccountConfigService _configService;
    private readonly string _accountId;
    private readonly CompositeDisposable _disposables = new();
    
    public ObservableCollection<OneDriveFolderNode> RootFolders { get; } = [];
    
    public ReactiveCommand<Unit, Unit> LoadTreeCommand { get; }
    public ReactiveCommand<Unit, Unit> StartSyncCommand { get; }
    
    // Auto-save: Debounced observable for checkbox changes
    private readonly Subject<Unit> _selectionChanged = new();
    
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    public SyncTreeViewModel(
        string accountId,
        IFolderTreeService folderTreeService,
        ISyncConfigRepository configRepository)
    {
        _accountId = accountId;
        _folderTreeService = folderTreeService;
        _configRepository = configRepository;
        
        LoadTreeCommand = ReactiveCommand.CreateFromTask(LoadRootFoldersAsync);
        StartSyncCommand = ReactiveCommand.CreateFromTask(StartSyncAsync);
        
        // Auto-save: Debounce checkbox changes and save to database
        _selectionChanged
            .Throttle(TimeSpan.FromMilliseconds(500))  // Wait 500ms after last change
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(_ => Observable.FromAsync(ct => SaveConfigurationAsync(ct)))
            .Subscribe()
            .DisposeWith(_disposables);
        
        // Load existing configuration
        LoadSavedConfiguration();
    }
    
    private async Task LoadRootFoldersAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        
        try
        {
            var rootFolders = await _folderTreeService.GetRootFoldersAsync(
                _accountId, 
                cancellationToken);
            
            RootFolders.Clear();
            foreach (var folder in rootFolders)
            {
                RootFolders.Add(folder);
                // Attach expand handler for lazy loading
                AttachExpandHandler(folder);
            }
            
            // Restore saved selections
            RestoreSavedSelections();
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void AttachExpandHandler(OneDriveFolderNode node)
    {
        // When node is expanded for first time, load children
        // This requires hooking into TreeViewItem.Expanded event
        // Alternative: use a command bound to expand gesture
    }
    
    public async Task LoadChildrenAsync(
        OneDriveFolderNode parentNode, 
        CancellationToken cancellationToken)
    {
        if (parentNode.Children.Count > 0)
            return; // Already loaded
        
        var children = await _folderTreeService.GetChildFoldersAsync(
            _accountId, 
            parentNode.Id, 
            cancellationToken);
        
        foreach (var child in children)
        {
            parentNode.Children.Add(child);
            AttachExpandHandler(child);
        }
    }
    
    private async Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        var selectedPaths = CollectSelectedPaths(RootFolders);
        
        var config = new SyncConfiguration
        {
            AccountId = _accountId,
            FolderSelections = selectedPaths
        };
        
        await _configService.SaveConfigurationAsync(config, cancellationToken);
    }
    
    private Dictionary<string, bool> CollectSelectedPaths(
        IEnumerable<OneDriveFolderNode> nodes)
    {
        var selections = new Dictionary<string, bool>();
        
        foreach (var node in nodes)
        {
            // For tri-state: only store explicitly selected/unselected leaf nodes
            // Parent states are computed, not stored
            if (!node.HasChildren && node.IsSelected.HasValue)
            {
                selections[node.Path] = node.IsSelected.Value;
            }
            else if (node.HasChildren)
            {
                // For parent nodes with explicit state (not computed), store it
                // This handles cases where entire branch is selected/unselected
                if (node.IsSelected == true)
                {
                    // All children selected - store parent selection
                    selections[node.Path] = true;
                }
                else if (node.IsSelected == false)
                {
                    // All children unselected - store parent unselection
                    selections[node.Path] = false;
                }
                else
                {
                    // Indeterminate - recurse to get individual child selections
                    var childSelections = CollectSelectedPaths(node.Children);
                    foreach (var kvp in childSelections)
                    {
                        selections[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        
        return selections;
    }
    
    private void LoadSavedConfiguration()
    {
        // Load from IAccountConfigService
    }
    
    private void RestoreSavedSelections()
    {
        // Apply saved IsSelected states to tree nodes
    }
    
    private async Task StartSyncAsync(CancellationToken cancellationToken)
    {
        // Save configuration first
        await SaveConfigurationAsync(cancellationToken);
        
        // Trigger sync engine (Phase 5)
        // Navigate back to main view or show sync progress
    }
}
```

**Checkbox Behavior** (Tri-State Implemented):
- **Checked (✓)**: All children selected (or leaf node explicitly checked)
- **Unchecked (☐)**: All children unselected (or leaf node explicitly unchecked)
- **Indeterminate (⊟)**: Some children selected, some unselected (mixed state)
- **Cascade behavior**: Checking/unchecking parent automatically cascades to all children
- **Propagation**: Child changes automatically update parent's tri-state
- **User Experience**: Users can quickly select/deselect entire branches or individual folders

**Testing Priority**: HIGH (tree traversal logic, selection state management)

**Specific Tests**:
- Verify lazy loading doesn't load children until expanded
- Test `CollectSelectedPaths` includes all nested selections correctly
- Verify saved configuration is restored correctly
- Test tri-state checkbox cascading:
  - Parent checked → all children checked
  - Parent unchecked → all children unchecked
  - Some children checked → parent indeterminate
  - All children checked → parent checked
- Test tri-state propagation through multiple levels (grandchildren)
- Verify indeterminate state visual display
- Test checkbox state after tree expansion (lazy-loaded children inherit parent state)

---

### Phase 5: Integration & Coordination

**Goal**: Connect all components and wire up navigation

#### 5.1 MainWindowViewModel Updates

```csharp
public sealed class MainWindowViewModel : ReactiveObject
{
    private readonly IMultiAccountManager _accountManager;
    private readonly IFolderTreeService _folderTreeService;
    private readonly CompositeDisposable _disposables = new();
    
    private ViewModelBase _currentView;
    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }
    
    public MainWindowViewModel(
        IMultiAccountManager accountManager,
        IFolderTreeService folderTreeService,
        IAccountConfigService configService)
    {
        _accountManager = accountManager;
        _folderTreeService = folderTreeService;
        
        // Start with account management view
        CurrentView = new AccountManagementViewModel(accountManager);
        
        // Setup navigation commands
        // TODO: Implement navigation routing
    }
    
    public void NavigateToSyncTree(string accountId)
    {
        var syncTreeViewModel = new SyncTreeViewModel(
            accountId,
            _folderTreeService,
            _configService);
        
        CurrentView = syncTreeViewModel;
        
        // Trigger initial load
        syncTreeViewModel.LoadTreeCommand.Execute().Subscribe();
    }
    
    public void NavigateToAccountManagement()
    {
        CurrentView = new AccountManagementViewModel(_accountManager);
    }
}
```

#### 5.2 MainWindow.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        x:Class="OneDriveClient.Views.MainWindow"
        Title="OneDrive Multi-Account Sync">
    
    <ContentControl Content="{Binding CurrentView}">
        <ContentControl.DataTemplates>
            <DataTemplate DataType="{x:Type vm:AccountManagementViewModel}">
                <views:AccountManagementView />
            </DataTemplate>
            <DataTemplate DataType="{x:Type vm:SyncTreeViewModel}">
                <views:SyncTreeView />
            </DataTemplate>
        </ContentControl.DataTemplates>
    </ContentControl>
    
</Window>
```

**Navigation Pattern**:
- Use `CurrentView` property with `ContentControl`
- DataTemplates map ViewModels to Views
- ViewModels raise events/commands to trigger navigation (or use interaction messaging)

---

### Phase 6: Database & Persistence

**Goal**: Implement SQLite database for settings, sync state, and delta tokens

#### 6.1 Database Schema & EF Core Setup

**Database Tables**:

```sql
-- Accounts table
CREATE TABLE Accounts (
    AccountId TEXT PRIMARY KEY,
    DisplayName TEXT NOT NULL,
    LocalSyncPath TEXT NOT NULL UNIQUE,
    IsAuthenticated INTEGER NOT NULL,
    LastSyncUtc TEXT,
    DeltaToken TEXT
);

-- SyncConfigurations table
CREATE TABLE SyncConfigurations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountId TEXT NOT NULL,
    FolderPath TEXT NOT NULL,
    IsSelected INTEGER NOT NULL,
    LastModifiedUtc TEXT NOT NULL,
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId) ON DELETE CASCADE
);

-- SyncStates table (for progress tracking)
CREATE TABLE SyncStates (
    AccountId TEXT PRIMARY KEY,
    Status INTEGER NOT NULL,
    TotalFiles INTEGER NOT NULL,
    CompletedFiles INTEGER NOT NULL,
    TotalBytes INTEGER NOT NULL,
    CompletedBytes INTEGER NOT NULL,
    MegabytesPerSecond REAL NOT NULL,
    EstimatedSecondsRemaining INTEGER,
    LastUpdateUtc TEXT,
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId) ON DELETE CASCADE
);

-- FileMetadata table (for tracking synced files and delta changes)
CREATE TABLE FileMetadata (
    Id TEXT PRIMARY KEY,           -- OneDrive file ID
    AccountId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Path TEXT NOT NULL,
    Size INTEGER NOT NULL,
    LastModifiedUtc TEXT NOT NULL,
    LocalPath TEXT NOT NULL,
    CTag TEXT,                     -- OneDrive change tag (content version identifier)
    ETag TEXT,                     -- OneDrive entity tag
    LocalHash TEXT,                -- SHA256 hash of local file (computed on local changes only)
    SyncStatus INTEGER NOT NULL,   -- NotSynced, Synced, PendingUpload, PendingDownload, Conflict
    LastSyncDirection INTEGER,     -- Download, Upload, or Both
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId) ON DELETE CASCADE
);

CREATE INDEX idx_file_account ON FileMetadata(AccountId);
CREATE INDEX idx_file_path ON FileMetadata(AccountId, Path);
```

**EF Core DbContext**:

```csharp
public sealed class SyncDbContext : DbContext
{
    public DbSet<AccountEntity> Accounts { get; set; } = null!;
    public DbSet<SyncConfigurationEntity> SyncConfigurations { get; set; } = null!;
    public DbSet<SyncStateEntity> SyncStates { get; set; } = null!;
    public DbSet<FileMetadataEntity> FileMetadata { get; set; } = null!;
    
    public SyncDbContext(DbContextOptions<SyncDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>()
            .HasKey(a => a.AccountId);
        
        modelBuilder.Entity<SyncConfigurationEntity>()
            .HasOne<AccountEntity>()
            .WithMany()
            .HasForeignKey(sc => sc.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // ... additional configuration
    }
}
```

**Repository Interfaces**:

```csharp
public interface IAccountRepository
{
    Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AccountInfo?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default);
    Task AddAsync(AccountInfo account, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountInfo account, CancellationToken cancellationToken = default);
    Task DeleteAsync(string accountId, CancellationToken cancellationToken = default);
}

public interface ISyncConfigRepository
{
    Task<SyncConfiguration?> GetAsync(string accountId, CancellationToken cancellationToken = default);
    Task SaveAsync(SyncConfiguration config, CancellationToken cancellationToken = default);
}

public interface ISyncStateRepository
{
    Task<SyncState?> GetAsync(string accountId, CancellationToken cancellationToken = default);
    Task SaveAsync(SyncState state, CancellationToken cancellationToken = default);
    IObservable<SyncState> ObserveSyncState(string accountId);
}

public interface IFileMetadataRepository
{
    Task<IReadOnlyList<FileMetadata>> GetAllForAccountAsync(string accountId, CancellationToken cancellationToken = default);
    Task UpsertAsync(FileMetadata metadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);
}
```

**Testing Priority**: HIGH (database operations, repository patterns)

---

### Phase 7: Two-Way Delta Sync Engine

**Goal**: Implement bidirectional Microsoft Graph Delta Sync with local file watching, pause/resume capability, and conflict detection

#### 7.1 File Watcher Service (Local Change Detection)

**Purpose**: Detect local file system changes (create, modify, delete) for upload to OneDrive

```csharp
public interface IFileWatcherService
{
    /// <summary>
    /// Starts watching a directory for changes.
    /// </summary>
    void StartWatching(string accountId, string localPath);
    
    /// <summary>
    /// Stops watching a directory.
    /// </summary>
    void StopWatching(string accountId);
    
    /// <summary>
    /// Observable stream of file change events.
    /// </summary>
    IObservable<FileChangeEvent> FileChanges { get; }
}

public sealed record FileChangeEvent
{
    public required string AccountId { get; init; }
    public required string LocalPath { get; init; }
    public required string RelativePath { get; init; }
    public FileChangeType ChangeType { get; init; }
    public DateTimeOffset DetectedUtc { get; init; } = DateTime.UtcNow;
}

public enum FileChangeType
{
    Created,
    Modified,
    Deleted,
    Renamed
}

public sealed class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = [];
    private readonly Subject<FileChangeEvent> _fileChanges = new();
    private readonly ILogger<FileWatcherService> _logger;
    
    public IObservable<FileChangeEvent> FileChanges => _fileChanges.AsObservable();
    
    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
    }
    
    public void StartWatching(string accountId, string localPath)
    {
        if (_watchers.ContainsKey(accountId))
        {
            _logger.LogWarning("Already watching path for account {AccountId}", accountId);
            return;
        }
        
        var watcher = new FileSystemWatcher(localPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName 
                         | NotifyFilters.DirectoryName 
                         | NotifyFilters.Size 
                         | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        
        // Debounce rapid changes (e.g., file being written)
        var changeBuffer = new Subject<FileSystemEventArgs>();
        changeBuffer
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(e => ProcessFileChange(accountId, localPath, e));
        
        watcher.Changed += (s, e) => changeBuffer.OnNext(e);
        watcher.Created += (s, e) => changeBuffer.OnNext(e);
        watcher.Deleted += (s, e) => _fileChanges.OnNext(CreateEvent(accountId, localPath, e, FileChangeType.Deleted));
        watcher.Renamed += (s, e) => _fileChanges.OnNext(CreateEvent(accountId, localPath, e, FileChangeType.Renamed));
        
        _watchers[accountId] = watcher;
        _logger.LogInformation("Started watching {Path} for account {AccountId}", localPath, accountId);
    }
    
    public void StopWatching(string accountId)
    {
        if (_watchers.Remove(accountId, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _logger.LogInformation("Stopped watching for account {AccountId}", accountId);
        }
    }
    
    private void ProcessFileChange(string accountId, string basePath, FileSystemEventArgs e)
    {
        var changeType = e.ChangeType == WatcherChangeTypes.Created 
            ? FileChangeType.Created 
            : FileChangeType.Modified;
        
        _fileChanges.OnNext(CreateEvent(accountId, basePath, e, changeType));
    }
    
    private static FileChangeEvent CreateEvent(
        string accountId, 
        string basePath, 
        FileSystemEventArgs e,
        FileChangeType changeType)
    {
        var relativePath = Path.GetRelativePath(basePath, e.FullPath);
        
        return new FileChangeEvent
        {
            AccountId = accountId,
            LocalPath = e.FullPath,
            RelativePath = relativePath,
            ChangeType = changeType
        };
    }
    
    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        _fileChanges.Dispose();
    }
}
```

**Key Features**:
- Monitors local sync directory for file changes
- Debounces rapid changes (500ms) to avoid processing partial writes
- Publishes observable stream of changes
- Per-account watching (isolated directories)
- Disposes cleanly on shutdown

**Testing Priority**: HIGH (file system events, debouncing)

---

#### 7.2 Delta Sync Service (Bidirectional)

**Microsoft Graph Delta Query**:

The [Delta Query](https://learn.microsoft.com/en-us/graph/delta-query-overview) allows efficient sync by only fetching changes since last sync:

```
GET /me/drive/root/delta?token={deltaToken}
```

Response includes:
- `@odata.deltaLink` - Token for next delta query
- `value[]` - Changed items (added, modified, deleted)

**IDeltaSyncEngine Interface**:

```csharp
public interface IDeltaSyncEngine
{
    /// <summary>
    /// Starts or resumes bidirectional sync for an account.
    /// Processes both remote changes (download) and local changes (upload).
    /// </summary>
    Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pauses ongoing sync (persists state for resume).
    /// </summary>
    Task PauseSyncAsync(string accountId);
    
    /// <summary>
    /// Observable stream of sync progress updates.
    /// </summary>
    IObservable<SyncState> SyncProgress(string accountId);
    
    /// <summary>
    /// Gets current sync status for an account.
    /// </summary>
    Task<SyncState?> GetSyncStateAsync(string accountId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all detected conflicts for an account.
    /// </summary>
    Task<IReadOnlyList<SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resolves a specific conflict with user's chosen strategy.
    /// </summary>
    Task ResolveConflictAsync(string accountId, string fileId, ConflictResolutionStrategy strategy, CancellationToken cancellationToken = default);
}

public sealed class DeltaSyncEngine : IDeltaSyncEngine
{
    private readonly IOneDriveApiService _apiService;
    private readonly IFileMetadataRepository _metadataRepo;
    private readonly ISyncStateRepository _stateRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ILogger<DeltaSyncEngine> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _activeSyncs = [];
    
    public async Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default)
    {
        // 1. Load account and delta token
        var account = await _accountRepo.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            throw new InvalidOperationException($"Account {accountId} not found");
        
        // 2. Create cancellable sync operation
        var syncCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeSyncs[accountId] = syncCts;
        
        // 3. Initialize or resume sync state
        var syncState = await _stateRepo.GetAsync(accountId, cancellationToken)
            ?? new SyncState { AccountId = accountId, Status = SyncStatus.Running };
        
        await _stateRepo.SaveAsync(syncState with { Status = SyncStatus.Running }, cancellationToken);
        
        try
        {
            // 4. Perform delta sync
            await PerformDeltaSyncAsync(account, syncState, syncCts.Token);
            
            // 5. Mark as completed
            await _stateRepo.SaveAsync(
                syncState with { Status = SyncStatus.Completed, LastUpdateUtc = DateTime.UtcNow },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Paused by user
            await _stateRepo.SaveAsync(
                syncState with { Status = SyncStatus.Paused },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for account {AccountId}", accountId);
            await _stateRepo.SaveAsync(
                syncState with { Status = SyncStatus.Failed },
                cancellationToken);
        }
        finally
        {
            _activeSyncs.Remove(accountId);
        }
    }
    
    private async Task PerformDeltaSyncAsync(
        AccountInfo account,
        SyncState initialState,
        CancellationToken cancellationToken)
    {
        var deltaToken = account.DeltaToken;
        var totalFiles = 0;
        var completedFiles = 0;
        var totalBytes = 0L;
        var completedBytes = 0L;
        var conflictsDetected = 0;
        var startTime = DateTime.UtcNow;
        
        // Phase 1: Process REMOTE changes (downloads from OneDrive)
        var deltaResult = await _apiService.GetDeltaChangesAsync(
            account.AccountId,
            deltaToken,
            cancellationToken);
        
        totalFiles = deltaResult.Changes.Count;
        
        foreach (var remoteChange in deltaResult.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Check if local file also changed (conflict detection)
            var localMetadata = await _metadataRepo.GetByPathAsync(
                account.AccountId,
                remoteChange.Path,
                cancellationToken);
            
            if (await HasLocalChangesAsync(localMetadata, account.LocalSyncPath, remoteChange.Path))
            {
                // CONFLICT: Both remote and local changed
                await RecordConflictAsync(account.AccountId, remoteChange, localMetadata!, cancellationToken);
                conflictsDetected++;
                continue; // Skip this file until user resolves
            }
            
            // No conflict - process remote change
            if (remoteChange.Deleted)
            {
                await HandleRemoteDeleteAsync(remoteChange, account.LocalSyncPath, cancellationToken);
            }
            else if (remoteChange.File is not null)
            {
                await HandleRemoteFileChangeAsync(remoteChange, account.LocalSyncPath, cancellationToken);
                completedBytes += remoteChange.Size;
            }
            
            completedFiles++;
            await UpdateProgressAsync(initialState, totalFiles, completedFiles, totalBytes, completedBytes, conflictsDetected, startTime, cancellationToken);
        }
        
        // Phase 2: Process LOCAL changes (uploads to OneDrive)
        var localChanges = await _metadataRepo.GetPendingUploadsAsync(account.AccountId, cancellationToken);
        totalFiles += localChanges.Count;
        
        foreach (var localChange in localChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Upload to OneDrive
            var uploadResult = await _apiService.UploadFileAsync(
                account.AccountId,
                localChange.LocalPath,
                localChange.Path,
                cancellationToken);
            
            // Update metadata with OneDrive's cTag
            await _metadataRepo.UpsertAsync(
                localChange with 
                { 
                    CTag = uploadResult.CTag,
                    ETag = uploadResult.ETag,
                    SyncStatus = SyncStatus.Synced,
                    LastSyncDirection = SyncDirection.Upload
                },
                cancellationToken);
            
            completedFiles++;
            completedBytes += localChange.Size;
            await UpdateProgressAsync(initialState, totalFiles, completedFiles, totalBytes, completedBytes, conflictsDetected, startTime, cancellationToken);
        }
        
        // Save new delta token for next sync
        await _accountRepo.UpdateAsync(
            account with { DeltaToken = deltaResult.NewDeltaToken, LastSyncUtc = DateTime.UtcNow },
            cancellationToken);
    }
    
    private async Task<bool> HasLocalChangesAsync(
        FileMetadata? metadata,
        string localSyncPath,
        string relativePath)
    {
        if (metadata is null)
            return false; // New file from OneDrive
        
        var localPath = Path.Combine(localSyncPath, relativePath);
        if (!File.Exists(localPath))
            return false; // Local file deleted (will be handled separately)
        
        var localInfo = new FileInfo(localPath);
        
        // Check if local file modified after last sync
        return localInfo.LastWriteTimeUtc > metadata.LastModifiedUtc;
    }
    
    private async Task RecordConflictAsync(
        string accountId,
        RemoteFileChange remoteChange,
        FileMetadata localMetadata,
        CancellationToken cancellationToken)
    {
        var conflict = new SyncConflict
        {
            FileId = remoteChange.Id,
            FilePath = remoteChange.Path,
            LocalModifiedUtc = localMetadata.LastModifiedUtc,
            RemoteModifiedUtc = remoteChange.LastModifiedUtc,
            LocalSize = localMetadata.Size,
            RemoteSize = remoteChange.Size,
            ResolvedStrategy = ConflictResolutionStrategy.Unresolved
        };
        
        // Store conflict for user resolution
        await _conflictRepo.AddAsync(conflict, cancellationToken);
        
        _logger.LogWarning(
            "Conflict detected for file {FilePath} in account {AccountId}",
            remoteChange.Path,
            accountId);
    }
    
    private async Task UpdateProgressAsync(
        SyncState initialState,
        int totalFiles,
        int completedFiles,
        long totalBytes,
        long completedBytes,
        int conflictsDetected,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        if (completedFiles % 10 != 0 && !ShouldUpdateProgress(startTime))
            return;
        
        var elapsed = DateTime.UtcNow - startTime;
        var mbps = elapsed.TotalSeconds > 0
            ? (completedBytes / 1_048_576.0) / elapsed.TotalSeconds
            : 0;
        
        var eta = mbps > 0 && totalBytes > completedBytes
            ? TimeSpan.FromSeconds((totalBytes - completedBytes) / 1_048_576.0 / mbps)
            : null;
        
        var updatedState = initialState with
        {
            TotalFiles = totalFiles,
            CompletedFiles = completedFiles,
            TotalBytes = totalBytes,
            CompletedBytes = completedBytes,
            ConflictsDetected = conflictsDetected,
            MegabytesPerSecond = mbps,
            EstimatedTimeRemaining = eta,
            LastUpdateUtc = DateTime.UtcNow
        };
        
        await _stateRepo.SaveAsync(updatedState, cancellationToken);
    }
    
    public async Task PauseSyncAsync(string accountId)
    {
        if (_activeSyncs.TryGetValue(accountId, out var cts))
        {
            await cts.CancelAsync();
        }
    }
    
    public IObservable<SyncState> SyncProgress(string accountId) =>
        _stateRepo.ObserveSyncState(accountId);
}
```

**Key Delta Sync Features**:
- **Bidirectional**: Downloads from OneDrive + uploads local changes
- Uses Graph API delta token to fetch only remote changes
- Uses `FileSystemWatcher` to detect local changes
- **Conflict Detection**: Compares remote cTag with local file timestamps
- Persists delta token to database for resume after app restart
- Tracks progress: files, bytes, MB/sec, ETA, conflicts
- Updates database every 10 files or 1 second for UI responsiveness
- Supports pause/resume via `CancellationToken`
- Handles file additions, modifications, and deletions (both directions)

**Change Detection Strategy**:
1. **Remote changes**: Use OneDrive's `cTag` (change tag) + `lastModifiedDateTime` + `size`
2. **Local changes**: Detect via `FileSystemWatcher`, compute SHA256 hash on upload
3. **Conflict detection**: If both remote cTag changed AND local file modified since last sync

**Why cTag over SHA256?**
- OneDrive provides `cTag` - it changes whenever content changes
- No need to compute SHA256 for downloaded files (saves CPU)
- SHA256 only computed for local files being uploaded
- Faster: cTag comparison vs reading entire file for hash

**Testing Priority**: HIGH (sync logic, conflict detection, upload/download, progress calculation, pause/resume)

---

#### 7.3 Conflict Resolution

**Purpose**: Handle scenarios where both local and remote files changed

```csharp
public interface IConflictResolver
{
    /// <summary>
    /// Applies the user's chosen resolution strategy to a conflict.
    /// </summary>
    Task ResolveAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default);
}

public sealed class ConflictResolver : IConflictResolver
{
    private readonly IOneDriveApiService _apiService;
    private readonly IFileMetadataRepository _metadataRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ILogger<ConflictResolver> _logger;
    
    public async Task ResolveAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepo.GetByIdAsync(conflict.AccountId, cancellationToken);
        if (account is null)
            throw new InvalidOperationException($"Account not found: {conflict.AccountId}");
        
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);
        
        switch (strategy)
        {
            case ConflictResolutionStrategy.KeepLocal:
                // Upload local version (overwrite remote)
                await _apiService.UploadFileAsync(
                    account.AccountId,
                    localPath,
                    conflict.FilePath,
                    cancellationToken);
                _logger.LogInformation("Resolved conflict: Kept local version of {Path}", conflict.FilePath);
                break;
            
            case ConflictResolutionStrategy.KeepRemote:
                // Download remote version (overwrite local)
                await _apiService.DownloadFileAsync(
                    account.AccountId,
                    conflict.FileId,
                    localPath,
                    cancellationToken);
                _logger.LogInformation("Resolved conflict: Kept remote version of {Path}", conflict.FilePath);
                break;
            
            case ConflictResolutionStrategy.KeepBoth:
                // Rename local file (add suffix), then download remote
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var extension = Path.GetExtension(localPath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(localPath);
                var directory = Path.GetDirectoryName(localPath)!;
                var renamedPath = Path.Combine(
                    directory,
                    $"{fileNameWithoutExt}_conflict_{timestamp}{extension}");
                
                File.Move(localPath, renamedPath);
                
                await _apiService.DownloadFileAsync(
                    account.AccountId,
                    conflict.FileId,
                    localPath,
                    cancellationToken);
                
                _logger.LogInformation(
                    "Resolved conflict: Renamed local to {Renamed}, downloaded remote to {Original}",
                    renamedPath,
                    localPath);
                break;
            
            case ConflictResolutionStrategy.Skip:
                // Do nothing - leave both versions as-is
                _logger.LogInformation("Resolved conflict: Skipped {Path}", conflict.FilePath);
                break;
            
            default:
                throw new ArgumentException($"Invalid strategy: {strategy}");
        }
        
        // Remove conflict from tracking
        await _conflictRepo.DeleteAsync(conflict.FileId, cancellationToken);
    }
}
```

**Testing Priority**: MEDIUM (file operations, strategy application)

---

### Phase 8: Sync Progress UI

**Goal**: Real-time progress display with pause/resume controls

#### 8.1 SyncProgressView

**UI Components**:
- Progress bar (percentage complete)
- File counter: "Syncing 45 of 200 files"
- Transfer rate: "5.2 MB/sec"
- ETA: "~2 minutes remaining"
- Pause/Resume button
- Cancel button

#### 8.2 SyncProgressViewModel

```csharp
public sealed class SyncProgressViewModel : ReactiveObject
{
    private readonly IDeltaSyncEngine _syncEngine;
    private readonly string _accountId;
    private readonly CompositeDisposable _disposables = new();
    
    private SyncState _currentState = new() 
    { 
        AccountId = "", 
        Status = SyncStatus.Idle 
    };
    
    public SyncState CurrentState
    {
        get => _currentState;
        set => this.RaiseAndSetIfChanged(ref _currentState, value);
    }
    
    // UI-friendly properties
    public string ProgressText => 
        $"Syncing {CurrentState.CompletedFiles} of {CurrentState.TotalFiles} files";
    
    public string DetailText =>
        $"↓ {CurrentState.FilesDownloading} downloading  ↑ {CurrentState.FilesUploading} uploading  ⚠ {CurrentState.ConflictsDetected} conflicts";
    
    public string TransferRateText => 
        $"{CurrentState.MegabytesPerSecond:F1} MB/sec";
    
    public string EtaText => CurrentState.EstimatedTimeRemaining.HasValue
        ? $"~{CurrentState.EstimatedTimeRemaining.Value.TotalMinutes:F0} minutes remaining"
        : "Calculating...";
    
    public bool HasConflicts => CurrentState.ConflictsDetected > 0;
    
    public double ProgressPercentage => CurrentState.TotalFiles > 0
        ? (double)CurrentState.CompletedFiles / CurrentState.TotalFiles * 100
        : 0;
    
    public bool IsPaused => CurrentState.Status == SyncStatus.Paused;
    public bool IsRunning => CurrentState.Status == SyncStatus.Running;
    
    public ReactiveCommand<Unit, Unit> PauseResumeCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    
    public SyncProgressViewModel(
        string accountId,
        IDeltaSyncEngine syncEngine)
    {
        _accountId = accountId;
        _syncEngine = syncEngine;
        
        // Subscribe to progress updates
        _syncEngine.SyncProgress(accountId)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(state =>
            {
                CurrentState = state;
                this.RaisePropertyChanged(nameof(ProgressText));
                this.RaisePropertyChanged(nameof(TransferRateText));
                this.RaisePropertyChanged(nameof(EtaText));
                this.RaisePropertyChanged(nameof(ProgressPercentage));
                this.RaisePropertyChanged(nameof(IsPaused));
                this.RaisePropertyChanged(nameof(IsRunning));
            })
            .DisposeWith(_disposables);
        
        PauseResumeCommand = ReactiveCommand.CreateFromTask(PauseResumeAsync);
        CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync);
    }
    
    private async Task PauseResumeAsync()
    {
        if (IsRunning)
        {
            await _syncEngine.PauseSyncAsync(_accountId);
        }
        else if (IsPaused)
        {
            await _syncEngine.StartSyncAsync(_accountId);
        }
    }
    
    private async Task CancelAsync()
    {
        await _syncEngine.PauseSyncAsync(_accountId);
        // Navigate back or close view
    }
}
```

**Testing Priority**: MEDIUM (UI coordination, observable subscriptions)

---

#### 8.3 Conflict Resolution UI

**Purpose**: Allow user to resolve detected conflicts

**UI Components**:
- List of conflicts with file paths
- For each conflict:
  - Local modified date vs Remote modified date
  - Local size vs Remote size
  - Radio buttons: Keep Local / Keep Remote / Keep Both / Skip
- "Resolve All" button (applies choices)

**ConflictResolutionViewModel**:

```csharp
public sealed class ConflictResolutionViewModel : ReactiveObject
{
    private readonly IDeltaSyncEngine _syncEngine;
    private readonly string _accountId;
    
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];
    
    public ReactiveCommand<Unit, Unit> ResolveAllCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    
    public ConflictResolutionViewModel(
        string accountId,
        IDeltaSyncEngine syncEngine,
        IReadOnlyList<SyncConflict> conflicts)
    {
        _accountId = accountId;
        _syncEngine = syncEngine;
        
        foreach (var conflict in conflicts)
        {
            Conflicts.Add(new ConflictItemViewModel(conflict));
        }
        
        ResolveAllCommand = ReactiveCommand.CreateFromTask(ResolveAllAsync);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }
    
    private async Task ResolveAllAsync()
    {
        foreach (var conflict in Conflicts)
        {
            if (conflict.SelectedStrategy == ConflictResolutionStrategy.Unresolved)
                continue; // User didn't choose
            
            await _syncEngine.ResolveConflictAsync(
                _accountId,
                conflict.FileId,
                conflict.SelectedStrategy,
                CancellationToken.None);
        }
        
        // Navigate back to sync progress
    }
    
    private void Cancel()
    {
        // Navigate back without resolving
    }
}

public sealed class ConflictItemViewModel : ReactiveObject
{
    public string FileId { get; }
    public string FilePath { get; }
    public DateTimeOffset LocalModifiedUtc { get; }
    public DateTimeOffset RemoteModifiedUtc { get; }
    public long LocalSize { get; }
    public long RemoteSize { get; }
    
    private ConflictResolutionStrategy _selectedStrategy = ConflictResolutionStrategy.Unresolved;
    public ConflictResolutionStrategy SelectedStrategy
    {
        get => _selectedStrategy;
        set => this.RaiseAndSetIfChanged(ref _selectedStrategy, value);
    }
    
    public ConflictItemViewModel(SyncConflict conflict)
    {
        FileId = conflict.FileId;
        FilePath = conflict.FilePath;
        LocalModifiedUtc = conflict.LocalModifiedUtc;
        RemoteModifiedUtc = conflict.RemoteModifiedUtc;
        LocalSize = conflict.LocalSize;
        RemoteSize = conflict.RemoteSize;
    }
}
```

**Testing Priority**: LOW (UI coordination)

---

### Phase 6 (Original): Persistence & Configuration

#### 6.2 Repository Implementations

**Note**: All storage now uses SQLite database via repositories (see Phase 6 above for schema and interfaces).

```csharp
public sealed class AccountRepository : IAccountRepository
{
    private readonly string _settingsPath;
    private readonly string _configBasePath;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public AccountConfigService()
    {
        var appDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "AStarOneDriveClient");
        
        Directory.CreateDirectory(appFolder);
        
        _settingsPath = Path.Combine(appFolder, "settings.json");
        _configBasePath = Path.Combine(appFolder, "configs");
        
        Directory.CreateDirectory(_configBasePath);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    public async Task<AppSettings> LoadSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();
        
        using var stream = File.OpenRead(_settingsPath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(
            stream, 
            _jsonOptions, 
            cancellationToken) 
            ?? new AppSettings();
    }
    
    public async Task SaveSettingsAsync(
        AppSettings settings, 
        CancellationToken cancellationToken = default)
    {
        using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(
            stream, 
            settings, 
            _jsonOptions, 
            cancellationToken);
    }
    
    public async Task<SyncConfiguration?> LoadConfigurationAsync(
        string accountId, 
        CancellationToken cancellationToken = default)
    {
        var configPath = GetConfigPath(accountId);
        
        if (!File.Exists(configPath))
            return null;
        
        using var stream = File.OpenRead(configPath);
        return await JsonSerializer.DeserializeAsync<SyncConfiguration>(
            stream, 
            _jsonOptions, 
            cancellationToken);
    }
    
    public async Task SaveConfigurationAsync(
        SyncConfiguration config, 
        CancellationToken cancellationToken = default)
    {
        var configPath = GetConfigPath(config.AccountId);
        
        using var stream = File.Create(configPath);
        await JsonSerializer.SerializeAsync(
            stream, 
            config, 
            _jsonOptions, 
            cancellationToken);
    }
    
    private string GetConfigPath(string accountId) =>
        Path.Combine(_configBasePath, $"{accountId}.json");
}
```

**Storage Structure**:
```
%LocalAppData%\AStarOneDriveClient\
├── sync.db                          # SQLite database (all settings, sync state, metadata)
└── token_cache.bin                  # MSAL token cache (encrypted, persistent)
```

**Database Schema**: See Phase 6 for complete schema (Accounts, SyncConfigurations, SyncStates, FileMetadata tables)

**Testing Priority**: HIGH (file I/O, serialization)

**Testing with MockFileSystem**:
```csharp
public class AccountConfigServiceShould
{
    [Fact]
    public async Task SaveAndLoadAppSettingsCorrectly()
    {
        var fileSystem = new MockFileSystem();
        var service = new AccountConfigService(fileSystem);
        
        var settings = new AppSettings
        {
            Accounts = 
            [
                new AccountInfo 
                { 
                    AccountId = "test123", 
                    DisplayName = "test@example.com",
                    LocalSyncPath = @"C:\Sync\test123"
                }
            ]
        };
        
        await service.SaveSettingsAsync(settings, CancellationToken.None);
        AppSettings loaded = await service.LoadSettingsAsync(CancellationToken.None);
        
        loaded.Accounts.Count.ShouldBe(1);
        loaded.Accounts[0].DisplayName.ShouldBe("test@example.com");
    }
}
```

---

## Testing Strategy

### Unit Test Coverage

| Component | Priority | Key Tests |
|-----------|----------|-----------|
| `MultiAccountManager` | HIGH | Add/remove accounts, enforce 3-account limit, login/logout state |
| `AccountManagementViewModel` | HIGH | Account collection updates, command execution, visibility properties |
| `FolderTreeService` | MEDIUM | Root folder fetching, child folder loading, API error handling |
| `SyncTreeViewModel` | HIGH | Tree loading, selection persistence, path collection |
| `AccountConfigService` | HIGH | Save/load settings, JSON serialization, file I/O |
| `OneDriveFolderNode` | LOW | Model validation |

### Integration Tests

- **Account + Tree Flow**: Login → Load tree → Save config → Verify persistence
- **Multi-Account Isolation**: Verify configs don't cross-contaminate
- **Graph API Integration**: Test against Microsoft Graph API (sandbox or mocked)

### Manual Testing Checklist

- [ ] Add 3 accounts successfully
- [ ] Verify 4th account attempt is prevented
- [ ] Login/logout for each account works independently
- [ ] Sync tree loads correct folders per account
- [ ] Checkbox states persist across sessions
- [ ] Remove account clears all associated data
- [ ] UI updates correctly as accounts change state
- [ ] Navigation between views works smoothly

---

## Technical Considerations

### MSAL Multi-Account Support

**Challenge**: MSAL token cache management for multiple accounts with persistent disk cache

**Solution**:
```csharp
// Use MSAL's built-in token cache serialization with PERSISTENT disk storage
var app = PublicClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, "common")
    .WithRedirectUri("http://localhost")
    .Build();

// CRITICAL: Enable persistent token cache to avoid repeated logins
var cacheDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AStarOneDriveClient");

var storageProperties = new StorageCreationPropertiesBuilder(
        "token_cache.bin",  // Persistent file
        cacheDir)
    .WithMacKeyChain("com.astar.onedrive", "AStarOneDriveClient")
    .WithLinuxKeyring(
        "com.astar.onedrive",
        "default",
        "AStarOneDrive",
        new KeyValuePair<string, string>("Version", "1"))
    .Build();

var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
cacheHelper.RegisterCache(app.UserTokenCache);

// Silent token acquisition (no login prompt if cached)
var accounts = await app.GetAccountsAsync();
var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

if (account is not null)
{
    // Try silent first (uses cached tokens)
    try
    {
        var result = await app.AcquireTokenSilent(scopes, account)
            .ExecuteAsync(cancellationToken);
        return result.AccessToken;
    }
    catch (MsalUiRequiredException)
    {
        // Token expired or not cached - interactive login required
        return await AcquireTokenInteractiveAsync(app, scopes);
    }
}
```

**Key Points**:
- **MSAL token cache MUST be persisted to disk** to avoid repeated login prompts
- Use `MsalCacheHelper` with `token_cache.bin` file for cross-session persistence
- MSAL manages multiple accounts natively
- Use `IAccount.HomeAccountId.Identifier` as persistent account identifier
- Token cache is shared but accounts are isolated
- Silent token acquisition requires passing specific account
- Always try `AcquireTokenSilent` first before prompting user

### Graph API Folder Hierarchy

**Efficient Folder Traversal**:
```csharp
// Avoid: Loading entire tree upfront (can be 1000s of folders)
// Instead: Lazy load on expand

// GET /me/drive/special/documents
// GET /me/drive/items/{folderId}/children?$filter=folder ne null&$select=id,name,folder

// For deep trees, consider pagination:
// $top=100&$skip=0
```

**Performance Optimization**:
- Cache loaded nodes to avoid re-fetching
- Use `$select` to minimize payload
- Implement cancellation for expand operations
- Consider background pre-loading for common folders

### Sync Directory Isolation

**Recommendation**: Use account email hash for directory names to avoid path length issues

```csharp
private static string GenerateSyncPath(string email, string rootPath)
{
    // Use SHA256 hash of email to create short, unique folder name
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(email));
    var hashString = Convert.ToHexString(hashBytes)[..12]; // First 12 chars
    
    return Path.Combine(rootPath, $"Account_{hashString}");
}

// Example output:
// C:\OneDriveSync\Account_A1B2C3D4E5F6\
```

**Benefits**:
- Unique per account
- Short path length
- No special character issues
- Deterministic (same email = same path)

### Checkbox Tree Patterns

**Tri-State Checkbox Implementation**:
- **Checked (✓)**: All children selected
- **Unchecked (☐)**: No children selected
- **Indeterminate (⊟)**: Some children selected (mixed state)

**Complete Implementation** (see `OneDriveFolderNode` above):

**Key Features**:
1. **Automatic Tri-State Computation**: Parent nodes compute state from children
2. **Cascading Selection**: Setting parent to true/false cascades to all descendants
3. **Upward Propagation**: Child changes trigger parent state recalculation
4. **Indeterminate Handling**: Null state indicates mixed selection

**Usage in ViewModel**:
```csharp
private void AttachExpandHandler(OneDriveFolderNode node)
{
    // Hook up child-to-parent notification
    foreach (var child in node.Children)
    {
        child.SelectionChanged += () => node.NotifyChildSelectionChanged();
        AttachExpandHandler(child);  // Recursive for all descendants
    }
}

// When loading tree nodes
private async Task LoadChildrenAsync(
    OneDriveFolderNode parentNode, 
    CancellationToken cancellationToken)
{
    if (parentNode.Children.Count > 0)
        return; // Already loaded
    
    var children = await _folderTreeService.GetChildFoldersAsync(
        _accountId, 
        parentNode.Id, 
        cancellationToken);
    
    foreach (var child in children)
    {
        parentNode.Children.Add(child);
        
        // Wire up parent notification
        child.SelectionChanged += () => parentNode.NotifyChildSelectionChanged();
        AttachExpandHandler(child);
    }
}
```

**Avalonia XAML Binding**:
```xml
<TreeView ItemsSource="{Binding RootFolders}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal">
                <!-- Avalonia CheckBox supports tri-state via IsThreeState and IsChecked (bool?) -->
                <CheckBox IsThreeState="True" 
                          IsChecked="{Binding IsSelected}" 
                          VerticalAlignment="Center" />
                <TextBlock Text="{Binding Name}" 
                           Margin="5,0,0,0" 
                           VerticalAlignment="Center" />
            </StackPanel>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

**Testing Scenarios**:
1. Check parent → all children become checked
2. Uncheck parent → all children become unchecked
3. Check some children → parent becomes indeterminate
4. Check remaining children → parent becomes checked
5. Deep nesting → tri-state propagates correctly through all levels

---

## UI/UX Design Notes

### Account Management View Mockup

```
┌──────────────────────────────────────────────────────┐
│  OneDrive Multi-Account Sync                         │
├──────────────────────────────────────────────────────┤
│                                                       │
│  Account: john.doe@example.com                       │
│  └─ [Logout]  [Show Sync Tree]                       │
│                                                       │
│  Account: jane.smith@company.com                     │
│  └─ [Logout]  [Show Sync Tree]                       │
│                                                       │
│  ┌───────────────────┐                               │
│  │  [+ Add Account]  │  (2 of 3 accounts used)       │
│  └───────────────────┘                               │
│                                                       │
└──────────────────────────────────────────────────────┘
```

### Sync Tree View Mockup (Tri-State Checkboxes)

```
┌──────────────────────────────────────────────────────┐
│  Sync Folders - john.doe@example.com                 │
├──────────────────────────────────────────────────────┤
│  Legend: [☑] Checked  [☐] Unchecked  [⊟] Indeterminate│
│                                                       │
│  [⊟] Documents (some items selected)                 │
│  │   [⊟] Work Projects                               │
│  │   │   [☑] Project A                               │
│  │   │   [☐] Project B (Archived)                    │
│  │   [☑] Personal (all items selected)               │
│  │                                                    │
│  [☑] Pictures (all items selected)                   │
│  │   [☑] Vacation 2025                               │
│  │   [☑] Family                                       │
│  │                                                    │
│  [☐] Videos (large files - skip all)                 │
│  [☑] Music                                            │
│  [☑] Desktop                                          │
│                                                       │
│  [Start Sync]  [Back]                                │
│  (Selections auto-saved)                              │
└──────────────────────────────────────────────────────┘
```

**Tri-State Behavior Examples**:
- Clicking **Documents** (currently indeterminate) → toggles to checked → all children checked
- Clicking **Documents** again → toggles to unchecked → all children unchecked
- Clicking **Documents** again → toggles back to checked
- Unchecking **Project A** manually → **Work Projects** becomes indeterminate → **Documents** stays indeterminate
- Checking **Project B** → **Work Projects** becomes checked → **Documents** becomes checked (all children now selected)

---

## Design Decisions (Resolved)

### 1. Sync Directory Selection ✅
**Decision**: User chooses sync directory per account via folder picker

**Requirements**:
- Each account must have a unique, non-overlapping sync directory
- Validate path uniqueness and writeability
- No nested sync directories allowed (e.g., Account2 sync path cannot be inside Account1's path)
- Path validation performed before account creation

### 2. Folder Selection Persistence ✅
**Decision**: Auto-save folder selections

**Implementation**:
- Use ReactiveUI's observable pattern to detect checkbox changes
- Debounce changes by 500ms to avoid excessive database writes
- Save to SQLite database automatically (no "Save" button needed)
- User sees folder selection immediately reflected in database

### 3. Sync Initiation ✅
**Decision**: Manual trigger via "Start Sync" button

**Rationale**:
- User may want to make multiple folder selections before starting
- Clear, explicit control over when sync begins
- Future: Add scheduled/automatic sync option

### 4. Account Removal ✅
**Decision**: Prompt user with dialog: "Keep synced files" or "Delete synced files"

**Implementation**:
- Show confirmation dialog with two options
- Default: Keep files (safer)
- If "Delete" chosen, recursively remove LocalSyncPath directory
- Always remove account from database and clear MSAL cache

### 5. Storage Mechanism ✅
**Decision**: SQLite database for all persistent data

**Data Stored**:
- Account settings and credentials
- Folder selection preferences per account
- Delta tokens for resuming sync
- Sync state (progress, pause status)
- File metadata for change detection

**Rationale**:
- Structured queries (filter by account, path, status)
- ACID transactions (atomic updates)
- Better performance than JSON files
- Native support for concurrent access

### 6. Sync Approach ✅
**Decision**: Microsoft Graph Delta Sync

**Implementation**:
- Use `/delta` endpoint to fetch only changes since last sync
- Persist delta token to database for resume capability
- Support pause/resume even after app restart
- Track progress: files, bytes, transfer rate, ETA

### 7. Token Caching ✅
**Decision**: MSAL token cache persisted to disk

**Implementation**:
- Use `MsalCacheHelper` with `token_cache.bin` file
- Tokens cached across app sessions
- Silent token acquisition to avoid repeated login prompts
- Only prompt for interactive login if token expired and refresh fails

### 8. Conflict Handling (Future)
**Question**: How to handle conflicts when file changed in both places?

**Recommendation**: Address in future sprint after core sync working

---

## Implementation Timeline

### Sprint 1 (Week 1-2): Foundation
- [x] **Step 1.1**: Create solution & projects
- [x] **Step 1.2**: Add NuGet packages & dependencies
- [x] **Step 1.3**: Setup database context & migrations (include WindowPreferences table)
- [x] **Step 1.4**: Create core models (records): `AccountInfo`, `SyncConfiguration`, `SyncState`, `SyncConflict`, `WindowPreferences`, enums
- [x] **Step 1.4.1**: Implement window position persistence service + tests (using DB)
- [x] **Step 1.5**: Create repository interfaces
- [x] **Step 1.6**: Implement repositories + tests
- [x] **Step 1.7**: Setup dependency injection

### Sprint 2 (Week 3-4): Account Management UI
- [x] **Step 2.1**: Create `AccountManagementViewModel` with ReactiveUI properties (account list, selected account, commands)
- [x] **Step 2.2**: Add ViewModel tests for property notifications and command execution
- [x] **Step 2.3**: Create `IAuthService` interface for MSAL authentication
- [x] **Step 2.4**: Implement `AuthService` with MSAL multi-account support + tests (also created `IAuthenticationClient`/`AuthenticationClient` wrappers and `MsalAuthResult` to enable testability around MSAL's sealed classes)
- [x] **Step 2.5**: Create `AccountManagementView.axaml` with basic UI (list, add/remove buttons)
- [x] **Step 2.6**: Wire ViewModel to repositories and authentication service (also added CancellationToken parameters to async methods)
- [x] **Step 2.7**: Integration testing (login/logout flows with in-memory database)
- [x] **Step 2.8**: Register services in DI and update MainWindow to show account management (AuthService registered as singleton with factory, MainWindow now displays AccountManagementView)

### Sprint 3 (Week 5-6): Folder Tree Service & Graph API Integration
- [x] **Step 3.1**: Create `OneDriveFolderNode` model with properties (Id, Name, Path, ParentId, IsFolder, Children collection)
- [x] **Step 3.2**: Create `IFolderTreeService` interface with methods (GetRootFoldersAsync, GetChildFoldersAsync, GetFolderHierarchyAsync)
- [x] **Step 3.3**: Create `IGraphApiClient` wrapper interface (similar to IAuthenticationClient pattern) with methods for testability
- [x] **Step 3.4**: Implement `GraphApiClient` wrapper around Microsoft.Graph SDK
- [x] **Step 3.5**: Implement `FolderTreeService` with Graph API integration + unit tests (with mocked IGraphApiClient) - 11 tests covering authentication checks, folder filtering, child loading, argument validation
- [x] **Step 3.6**: Add integration tests using real Graph API calls (requires authenticated IAuthService) - 5 skipped integration tests (GetRootFoldersFromRealOneDriveAccount, GetChildFoldersFromRealOneDriveFolder, GetFolderHierarchyWithLimitedDepth, HandleEmptyFoldersGracefully, GraphApiClientCanAccessDrive) - tests use TokenProvider helper class for authentication. Fixed ClientId configuration in appsettings.json - successful real OneDrive authentication and Graph API integration verified
- [x] **Step 3.7**: Create helper method to build hierarchical tree from flat DriveItem list - Created FolderTreeBuilder static class with BuildTree() and MergeIntoTree() methods + 16 unit tests covering tree building, sorting, merging, path construction, null handling, and argument validation
- [x] **Step 3.8**: Manual testing against real OneDrive account (verify folder structure loads correctly) - Verified working with real OneDrive account

### Sprint 4 (Week 7-8): Sync Tree UI (Tri-State Checkboxes)
- [x] **Step 4.1**: Add tri-state selection properties to OneDriveFolderNode (IsSelected nullable bool, SelectionState enum: Unchecked/Checked/Indeterminate) - Created SelectionState enum, updated OneDriveFolderNode to inherit from ReactiveObject with SelectionState and IsSelected properties using RaiseAndSetIfChanged + 14 unit tests covering property change notifications, all selection states, and observable collection behavior
- [x] **Step 4.2**: Create ISyncSelectionService interface for managing folder selection state - Created interface with SetSelection, UpdateParentStates, GetSelectedFolders, ClearAllSelections, and CalculateStateFromChildren methods
- [x] **Step 4.3**: Implement SyncSelectionService with cascading selection logic (parent→children) + unit tests - Implemented service with cascading logic, indeterminate state calculation + 22 unit tests covering selection cascading, parent state updates, upward propagation, state calculation, and argument validation
- [x] **Step 4.4**: Implement upward propagation logic (children→parent indeterminate state calculation) + unit tests - Already implemented in Step 4.3's UpdateParentStates method
- [x] **Step 4.5**: Create SyncTreeViewModel with IFolderTreeService and ISyncSelectionService dependencies + unit tests - Created ViewModel with LoadFoldersCommand, LoadChildrenCommand, ToggleSelectionCommand, ClearSelectionsCommand + 19 unit tests covering async folder loading, error handling, selection toggling, property change notifications, and argument validation
- [x] **Step 4.6**: Create SyncTreeView.axaml with TreeView, tri-state CheckBox, and hierarchical data template - View implemented with TreeView, CheckBox bindings, and proper XAML data templates
- [x] **Step 4.7**: Wire ViewModel to View, register in DI, and integrate into MainWindow - ViewModel wired to view, services registered in ServiceConfiguration, MainWindow updated with navigation support
- [x] **Step 4.8**: Integration tests with AccountManagementView and manual testing of selection behavior - Integration tests created (SyncTreeViewModelPersistenceIntegrationShould), manual testing completed, folder selection persistence working

### Sprint 5 (Week 9-10): Database & Persistence
- [x] Design SQLite database schema - Schema designed with AccountInfo, SyncConfigurations, WindowPreferences, FileMetadata tables
- [x] Setup EF Core with migrations - EF Core 9.0 configured, InitialCreate migration created (20260105204114), database stored in LocalApplicationData
- [x] Implement repository interfaces - IAccountRepository, ISyncConfigRepository implemented with full CRUD operations
- [x] Test database operations (CRUD, queries) - Repository tests created, all operations tested and passing
- [x] Migrate from in-memory to database storage - Fully migrated to SQLite database at %LocalAppData%\AStarOneDriveClient\sync.db

### Sprint 6 (Week 11-13): Delta Sync Engine (Bidirectional)
- [ ] Implement `IFileWatcherService` for local change detection - NOT YET IMPLEMENTED (future work - on-demand scanning via LocalFileScanner works for now)
- [x] Implement `ISyncEngine` interface - ✅ COMPLETE: ISyncEngine with downloads, uploads, and deletions working
- [x] Integrate Graph API for downloads - ✅ COMPLETE: Real file downloads with IGraphApiClient.DownloadFileAsync, streaming, SHA256 verification
- [x] Add upload logic for local changes - ✅ COMPLETE: Real Graph API uploads implemented with UploadFileAsync (small <4MB and large ≥4MB resumable sessions)
- [x] Implement conflict detection (cTag + timestamp comparison) - ✅ COMPLETE: RemoteChangeDetector uses CTag, ETag, LastModifiedUtc, Size for change detection
- [x] Add progress tracking and state persistence - ✅ COMPLETE: SyncProgress observable, FileMetadata in database with SyncStatus, LastSyncDirection
- [x] Implement bidirectional deletion sync - ✅ COMPLETE: Files deleted from OneDrive are deleted locally; files deleted locally will be deleted from OneDrive (DeleteFileAsync implemented)
- [x] Implement pause/resume with cancellation tokens - ✅ COMPLETE: CancellationToken support throughout SyncEngine with proper cleanup and state persistence
- [x] Test bidirectional sync with real OneDrive data - ✅ COMPLETE: Downloads, uploads, and deletions working with real OneDrive account
- [x] Test conflict scenarios - ✅ COMPLETE: Manual testing completed with all conflict resolution strategies

### Sprint 7 (Week 14-15): Sync Progress & Conflict Resolution UI
- [x] Create `ConflictResolutionView.axaml` - ✅ COMPLETE: View with DataGrid showing all conflicts, columns for file path, local/remote timestamps and sizes, resolution strategy dropdown
- [x] Implement `ConflictResolutionViewModel` - ✅ COMPLETE: ViewModel with ConflictItems observable collection, ResolveConflictsCommand, CancelCommand, full ReactiveUI integration
- [x] Implement conflict resolution strategies - ✅ COMPLETE: KeepLocal, KeepRemote, KeepBoth (with rename), Skip - all strategies implemented in ConflictResolver service
- [x] Integrate conflict resolution into SyncEngine - ✅ COMPLETE: Bidirectional conflict detection, first-sync conflict detection, conflict resolution with IConflictResolver service
- [x] Test conflict resolution workflows - ✅ COMPLETE: Manual testing verified all strategies working correctly (KeepLocal, KeepRemote, KeepBoth with rename, Skip)
- [x] Implement detailed sync logging - ✅ COMPLETE: Two-table system with SyncSessionLog (session summaries) and FileOperationLog (individual file operations with timestamps, hashes, reasons)
- [x] Add concurrent sync protection - ✅ COMPLETE: Interlocked-based guard prevents duplicate sync execution
- [x] Fix upload timestamp synchronization - ✅ COMPLETE: Local file timestamps synchronized to OneDrive after upload to prevent false change detection
- [x] Add extensive debug logging - ✅ COMPLETE: Debug.WriteLine statements throughout sync flow for troubleshooting (kept for future debugging needs)
- [x] Create `SyncProgressView.axaml` - ✅ COMPLETE: Full UI with progress bar, file counts, transfer details, conflict warnings
- [x] Implement `SyncProgressViewModel` with live updates - ✅ COMPLETE: Real-time progress updates with reactive bindings, property notifications, status messages
- [x] Add pause/resume UI controls - ✅ COMPLETE: Start Sync and Pause buttons with proper state management via IsSyncing property
- [x] Test progress calculations (ETA, MB/sec) - ✅ COMPLETE: Implemented transfer speed calculation (MB/s) with 10-sample smoothing and ETA based on remaining bytes
- [x] Ensure UI updates don't block sync - ✅ COMPLETE: All async operations with CancellationToken, ReactiveUI scheduling ensures UI responsiveness

### Sprint 8 (Week 16-17): Integration & Navigation
- [x] Update `MainWindowViewModel` for navigation - MainWindowViewModel implemented with coordination between AccountManagementViewModel and SyncTreeViewModel, reactive binding wires selected account to sync tree
- [x] Wire up all views in `MainWindow.axaml` - MainWindow.axaml implemented with 2-column grid layout: AccountManagementView (left) and SyncTreeView (right)
- [x] Integrate sync engine with folder selection - SyncEngine integrated with database folder selection loading, selected folders retrieved from SyncConfigurations table, sync operates on selected folders only
- [x] Integrate file watcher - ✅ COMPLETE: FileWatcherService implemented with FileSystemWatcher, 500ms debouncing, AutoSyncCoordinator manages automatic sync triggers with 2-second buffering, 14 unit tests passing
- [x] Test complete user flow (add account → login → tree → sync) - End-to-end flow tested: login working, folder tree loading, folder selection persisting, sync downloading files successfully
- [x] Fix any integration issues - Major integration issues resolved: authentication accountId parameter refactoring, Graph API Root property handling, folder selection persistence (empty string filtering), LocalPath population, upload ID preservation

### Sprint 9 (Week 18-19): Polish & Testing
- [ ] End-to-end testing (full bidirectional sync workflows)
- [ ] Test pause/resume across app restarts
- [ ] Test conflict resolution with various strategies
- [x] Test file watcher with rapid changes - ✅ Manual testing completed successfully
- [ ] Performance optimization (large file uploads/downloads)
- [ ] UI/UX refinements
- [ ] Documentation
- [ ] EF Core migrations and database versioning

---

## Future Enhancements (Post-MVP)

1. **Smart Conflict Resolution**: Auto-resolve based on rules (e.g., always keep newer file)
2. **Selective Sync Editing**: Modify folder selection after initial setup (re-sync or cleanup)
3. **Account Nicknames**: Custom labels for accounts (in addition to email)
4. **Bandwidth Throttling**: Limit sync speed (MB/sec cap)
5. **Scheduled Sync**: Automatic sync at intervals (hourly, daily, etc.)
6. **OneDrive Business Support**: Support both personal and business accounts in same app
7. **Shared Folders**: Sync shared OneDrive folders (requires additional Graph API permissions)
8. **Selective File Type Filtering**: Exclude certain file types (e.g., skip .tmp, .log files)
9. **Sync History Log**: View past sync operations and errors
10. **Dark Mode**: Theme support (Avalonia already has primitives)
11. **Upload-Only or Download-Only Modes**: One-way sync options
12. **LAN Sync**: Detect and prioritize local network transfers for same files

---

## Dependencies & Libraries

### Required NuGet Packages
- **Microsoft.Identity.Client** (MSAL): ^4.60+
- **Microsoft.Identity.Client.Extensions.Msal**: ^4.60+ (for token cache persistence)
- **Microsoft.Graph** (Graph SDK): ^5.50+
- **Microsoft.EntityFrameworkCore**: ^9.0+
- **Microsoft.EntityFrameworkCore.Sqlite**: ^9.0+
- **Microsoft.EntityFrameworkCore.Design**: ^9.0+ (for migrations)
- **Avalonia**: ^11.0+
- **Avalonia.ReactiveUI**: ^11.0+
- **System.Reactive**: ^6.0+
- **Microsoft.Extensions.Logging**: Built-in
- **Microsoft.Extensions.DependencyInjection**: Built-in

### Development/Testing
- **xUnit**: ^3.0+
- **Shouldly**: ^4.2+
- **NSubstitute**: ^5.1+
- **System.IO.Abstractions.TestingHelpers**: ^21.0+ (MockFileSystem)

---

## Success Criteria

### Functional Requirements
- [x] User can add up to 3 OneDrive accounts
- [x] Each account has independent login/logout with persistent token cache
- [x] User selects unique sync directory per account
- [x] Window position and size preferences persist across sessions
- [x] Sync tree displays OneDrive folder structure
- [x] User can select/deselect folders with checkboxes (auto-saved)
- [x] Folder selections persist in SQLite database across app restarts
- [x] All folders default to checked
- [x] UI adapts based on number of accounts
- [x] Bidirectional sync: downloads from OneDrive + uploads local changes
- [x] Conflict detection when both local and remote change
- [x] User can resolve conflicts (keep local, keep remote, keep both, skip)
- [x] Pause/resume sync (persists across app restarts)
- [x] Real-time progress: files, bytes, MB/sec, ETA, conflicts
- [x] Account deletion prompts to keep or delete local files
- [x] EF Core migrations for database schema versioning

### Non-Functional Requirements
- **Performance**: Folder tree loads in <2 seconds for typical user
- **Reliability**: No crashes on network errors or API failures
- **Usability**: Intuitive UI, no user training required
- **Testability**: >80% code coverage for core logic
- **Cross-Platform**: Works on Windows, macOS, Linux

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| MSAL multi-account complexity | High | Medium | Thorough testing, MSAL docs review |
| Graph API rate limits | Medium | Low | Implement retry logic, cache aggressively |
| Large folder hierarchies (performance) | Medium | Medium | Lazy loading, virtualization |
| Token expiration during tree load | Low | Medium | Implement token refresh before Graph calls |
| Path length limits (Windows) | Low | Low | Use short hashed directory names |
| User confusion with 3 accounts | Medium | Low | Clear labeling, visual separation |

---

## Conclusion

This implementation plan provides a structured approach to building the multi-account OneDrive sync solution with improved UX. The phased approach allows for incremental development and testing, ensuring each component is solid before integration.

**Next Steps**:
1. Review and approve this plan
2. Clarify open questions (Sections "Open Questions & Decisions Needed")
3. Set up project tracking (Jira/Azure DevOps/GitHub Projects)
4. Begin Sprint 1 implementation

**Questions? Clarifications?**
- How should we handle existing accounts from V2 (if any)?
- Do we need data migration from previous version?
- Any specific Graph API permissions constraints?
- Preferred logging/telemetry solution?

---

**Document Version**: 1.0  
**Last Updated**: January 5, 2026  
**Status**: Awaiting Approval
