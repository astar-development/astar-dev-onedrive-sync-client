using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository) : ObservableObject
{
    private readonly OneDriveAccount    _account      = account;
    private readonly IAuthService       _authService  = authService;
    private readonly IGraphService      _graphService = graphService;
    private readonly IAccountRepository _repository   = repository;
    private          string?            _accessToken;
    private          string?            _driveId;

    // ── Display ───────────────────────────────────────────────────────────

    public string AccountId => _account.Id;
    public string DisplayName => _account.DisplayName;
    public string Email => _account.Email;

    public string TabLabel => _account.DisplayName.Length > 0
                                 ? _account.DisplayName
                                 : _account.Email;

    public int AccentIndex => _account.AccentIndex;

    public Color AccentColor => AccountCardViewModel.PaletteColor(_account.AccentIndex);

    // ── Tree ──────────────────────────────────────────────────────────────

    public ObservableCollection<FolderTreeNodeViewModel> RootFolders { get; } = [];

    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private string _loadError    = string.Empty;
    [ObservableProperty] private bool   _hasLoadError;

    // ── Tab state ─────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isActiveTab;

    // ── Events ────────────────────────────────────────────────────────────

    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsLoading)
            return;

        IsLoading = true;
        HasLoadError = false;
        LoadError = string.Empty;
        RootFolders.Clear();

        try
        {
            AuthResult authResult = await _authService.AcquireTokenSilentAsync(_account.Id);

            if(authResult.IsError)
            {
                LoadError = authResult.ErrorMessage ?? "Authentication failed.";
                HasLoadError = true;
                return;
            }

            _accessToken = authResult.AccessToken!;

            // Resolve and cache drive ID — used by all tree nodes for lazy loading
            _driveId = await _graphService.GetDriveIdAsync(_accessToken);

            List<DriveFolder> folders = await _graphService.GetRootFoldersAsync(_accessToken);

            foreach(DriveFolder f in folders)
            {
                FolderSyncState syncState = _account.SelectedFolderIds.Contains(f.Id)
                    ? FolderSyncState.Included
                    : FolderSyncState.Excluded;

                var node = new FolderTreeNode(
                    Id:          f.Id,
                    Name:        f.Name,
                    ParentId:    f.ParentId,
                    AccountId:   _account.Id,
                    SyncState:   syncState,
                    HasChildren: true);

                var vm = new FolderTreeNodeViewModel(
                    node, _graphService, _accessToken, _driveId);

                vm.IncludeToggled += OnIncludeToggled;
                vm.ViewActivityRequested += OnViewActivityRequested;
                vm.OpenInFileManagerRequested += OnOpenInFileManager;

                RootFolders.Add(vm);
            }
        }
        catch(Exception ex)
        {
            LoadError = $"Failed to load folders: {ex.Message}";
            HasLoadError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async void OnIncludeToggled(object? sender, FolderTreeNodeViewModel node)
    {
        if(node.IsIncluded)
        {
            if(!_account.SelectedFolderIds.Contains(node.Id))
                _account.SelectedFolderIds.Add(node.Id);
        }
        else
        {
            _ = _account.SelectedFolderIds.Remove(node.Id);
        }

        // Persist — only root folders tracked for now; sub-folder tracking in step 6
        var entity = new AccountEntity
        {
            Id           = _account.Id,
            DisplayName  = _account.DisplayName,
            Email        = _account.Email,
            AccentIndex  = _account.AccentIndex,
            IsActive     = _account.IsActive,
            DeltaLink    = _account.DeltaLink,
            LastSyncedAt = _account.LastSyncedAt,
            QuotaTotal   = _account.QuotaTotal,
            QuotaUsed    = _account.QuotaUsed,
            SyncFolders  = [.. RootFolders
                .Where(f => f.IsIncluded)
                .Select(f => new SyncFolderEntity
                {
                    FolderId   = f.Id,
                    FolderName = f.Name,
                    AccountId  = _account.Id
                })]
        };

        await _repository.UpsertAsync(entity);
    }

    private void OnViewActivityRequested(object? sender, FolderTreeNodeViewModel node)
        => ViewActivityRequested?.Invoke(this, node);

    private static void OnOpenInFileManager(object? sender, FolderTreeNodeViewModel node)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", node.Name);

        if(!Directory.Exists(path))
            return;

        var opener = OperatingSystem.IsWindows() ? "explorer"
                   : OperatingSystem.IsMacOS()   ? "open"
                   : "xdg-open";

        _ = System.Diagnostics.Process.Start(opener, path);
    }
}
