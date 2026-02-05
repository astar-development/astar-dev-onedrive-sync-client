using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;

/// <summary>
/// ViewModel for the Add Account UI flow.
/// Manages authentication state and orchestrates account creation after successful authentication.
/// </summary>
public class AddAccountViewModel : ReactiveObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountCreationService _accountCreationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAccountViewModel"/> class.
    /// </summary>
    /// <param name="authenticationService">Service for handling OAuth authentication.</param>
    /// <param name="accountCreationService">Service for creating accounts after successful authentication.</param>
    public AddAccountViewModel(IAuthenticationService authenticationService, IAccountCreationService accountCreationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _accountCreationService = accountCreationService ?? throw new ArgumentNullException(nameof(accountCreationService));

        AuthenticateCommand = ReactiveCommand.CreateFromTask(AuthenticateAsync);
    }

    /// <summary>
    /// Gets or sets the current status message displayed to the user.
    /// </summary>
    public string StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets or sets the current error message displayed to the user.
    /// </summary>
    public string ErrorMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether authentication is in progress.
    /// </summary>
    public bool IsAuthenticating
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether account creation is in progress.
    /// </summary>
    public bool IsCreatingAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the account that was successfully created.
    /// </summary>
    public Account? CreatedAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the command to initiate authentication flow.
    /// </summary>
    public ICommand AuthenticateCommand { get; }

    /// <summary>
    /// Executes the authentication and account creation workflow.
    /// </summary>
    private async Task AuthenticateAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            StatusMessage = "Starting authentication...";
            IsAuthenticating = true;
            CreatedAccount = null;

            Result<AuthToken, AuthenticationError> authResult = await _authenticationService.AuthenticateAsync();

            if (authResult is Result<AuthToken, AuthenticationError>.Ok okAuth)
            {
                StatusMessage = "Authentication successful! Creating account...";
                IsAuthenticating = false;
                IsCreatingAccount = true;

                Result<Account, AccountCreationError> creationResult = await _accountCreationService.CreateAccountAsync(okAuth.Value);

                if (creationResult is Result<Account, AccountCreationError>.Ok okAccount)
                {
                    CreatedAccount = okAccount.Value;
                    StatusMessage = $"Account created successfully for {okAccount.Value.HashedEmail}";
                    IsCreatingAccount = false;
                }
                else if (creationResult is Result<Account, AccountCreationError>.Error errAccount)
                {
                    ErrorMessage = MapAccountCreationError(errAccount.Reason);
                    StatusMessage = "Account creation failed";
                    IsCreatingAccount = false;
                }
            }
            else if (authResult is Result<AuthToken, AuthenticationError>.Error errAuth)
            {
                ErrorMessage = MapAuthenticationError(errAuth.Reason);
                StatusMessage = "Authentication failed";
                IsAuthenticating = false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unexpected error: {ex.Message}";
            StatusMessage = "An unexpected error occurred";
            IsAuthenticating = false;
            IsCreatingAccount = false;
        }
    }

    /// <summary>
    /// Maps authentication errors to user-friendly messages.
    /// </summary>
    private static string MapAuthenticationError(AuthenticationError error)
        => error switch
        {
            AuthenticationError.Cancelled => "Authentication was cancelled. Please try again.",
            AuthenticationError.TimedOut => "Authentication timed out. Please check your internet connection and try again.",
            AuthenticationError.NetworkError => "Network error occurred. Please check your internet connection.",
            AuthenticationError.ServiceError => "Microsoft authentication service is temporarily unavailable. Please try again later.",
            AuthenticationError.ConfigurationError => "Authentication configuration error. Please contact support.",
            AuthenticationError.UnexpectedError => "An unexpected authentication error occurred. Please try again.",
            _ => $"Authentication failed with error: {error}"
        };

    /// <summary>
    /// Maps account creation errors to user-friendly messages.
    /// </summary>
    private static string MapAccountCreationError(AccountCreationError error)
        => error switch
        {
            AccountCreationError.AccountAlreadyExists => "An account with this email address already exists.",
            AccountCreationError.GraphApiError => "Unable to retrieve your profile information. Please try again.",
            AccountCreationError.TokenStorageError => "Unable to securely store your authentication token. Please try again.",
            AccountCreationError.RepositoryError => "Unable to save your account information. Please try again.",
            AccountCreationError.ValidationError => "Account validation failed. Please try again.",
            AccountCreationError.UnexpectedError => "An unexpected error occurred while creating your account. Please try again.",
            _ => $"Account creation failed with error: {error}"
        };
}
