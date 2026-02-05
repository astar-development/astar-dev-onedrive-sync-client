using System.Windows.Input;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;

/// <summary>
/// ViewModel for editing account settings.
/// Manages validation and updates for sync directory, concurrency, debug logging, and bandwidth settings.
/// </summary>
public class EditAccountViewModel : ReactiveObject
{
    private readonly IAccountManagementService _accountManagementService;
    private string _hashedAccountId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditAccountViewModel"/> class.
    /// </summary>
    /// <param name="accountManagementService">Service for managing account updates.</param>
    public EditAccountViewModel(IAccountManagementService accountManagementService)
    {
        _accountManagementService = accountManagementService ?? throw new ArgumentNullException(nameof(accountManagementService));
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
    }

    /// <summary>
    /// Gets or sets the hashed account identifier.
    /// </summary>
    public string HashedAccountId
    {
        get => _hashedAccountId;
        set => this.RaiseAndSetIfChanged(ref _hashedAccountId, value);
    }

    /// <summary>
    /// Gets or sets the home sync directory path.
    /// </summary>
    public string? HomeSyncDirectory
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the maximum concurrent operations (1-unlimited).
    /// </summary>
    public int MaxConcurrent
    {
        get;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            ValidateMaxConcurrent();
        }
    } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled.
    /// </summary>
    public bool DebugLoggingEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the maximum bandwidth in KB/s (null for unlimited).
    /// </summary>
    public int? MaxBandwidthKBps
    {
        get;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            ValidateMaxBandwidth();
        }
    }

    /// <summary>
    /// Gets or sets the validation error message for MaxConcurrent.
    /// </summary>
    public string? MaxConcurrentError
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the validation error message for MaxBandwidthKBps.
    /// </summary>
    public string? MaxBandwidthError
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the error message from save operation.
    /// </summary>
    public string? ErrorMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the last save was successful.
    /// </summary>
    public bool SaveSuccessful
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether save operation is in progress.
    /// </summary>
    public bool IsSaving
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the command to save account settings.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Loads an account's settings into the ViewModel.
    /// </summary>
    public void LoadAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        HashedAccountId = account.HashedAccountId;
        HomeSyncDirectory = account.HomeSyncDirectory;
        MaxConcurrent = account.MaxConcurrent;
        DebugLoggingEnabled = account.DebugLoggingEnabled;
        MaxBandwidthKBps = account.MaxBandwidthKBps;
        ErrorMessage = null;
        SaveSuccessful = false;
    }

    private void ValidateMaxConcurrent()
    {
        if (MaxConcurrent < 1)
        {
            MaxConcurrentError = "Maximum concurrent operations must be at least 1.";
        }
        else
        {
            MaxConcurrentError = null;
        }
    }

    private void ValidateMaxBandwidth()
    {
        if (MaxBandwidthKBps.HasValue && MaxBandwidthKBps.Value < 0)
        {
            MaxBandwidthError = "Maximum bandwidth cannot be negative.";
        }
        else
        {
            MaxBandwidthError = null;
        }
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;
        SaveSuccessful = false;

        if (!string.IsNullOrWhiteSpace(MaxConcurrentError) || !string.IsNullOrWhiteSpace(MaxBandwidthError))
        {
            ErrorMessage = "Please fix validation errors before saving.";
            return;
        }

        IsSaving = true;

        try
        {
            Result<Account, AccountManagementError> homeSyncResult = await _accountManagementService.UpdateHomeSyncDirectoryAsync(HashedAccountId, HomeSyncDirectory);
            if (homeSyncResult is Result<Account, AccountManagementError>.Error)
            {
                ErrorMessage = "Failed to update sync directory.";
                return;
            }

            Result<Account, AccountManagementError> maxConcurrentResult = await _accountManagementService.UpdateMaxConcurrentAsync(HashedAccountId, MaxConcurrent);
            if (maxConcurrentResult is Result<Account, AccountManagementError>.Error)
            {
                ErrorMessage = "Failed to update concurrent operations setting.";
                return;
            }

            Result<Account, AccountManagementError> debugLoggingResult = await _accountManagementService.UpdateDebugLoggingAsync(HashedAccountId, DebugLoggingEnabled);
            if (debugLoggingResult is Result<Account, AccountManagementError>.Error)
            {
                ErrorMessage = "Failed to update debug logging setting.";
                return;
            }

            Result<Account, AccountManagementError> bandwidthResult = await _accountManagementService.UpdateMaxBandwidthKBpsAsync(HashedAccountId, MaxBandwidthKBps);
            if (bandwidthResult is Result<Account, AccountManagementError>.Error)
            {
                ErrorMessage = "Failed to update bandwidth limit.";
                return;
            }

            SaveSuccessful = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
