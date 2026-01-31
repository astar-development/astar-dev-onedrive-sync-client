using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using AStar.Dev.OneDrive.Client.Models;
using AStar.Dev.OneDrive.Client.Services.OneDriveServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Client.Tests.Integration.Services.OneDriveServices;

/// <summary>
///     Helper class for providing bearer token authentication to GraphServiceClient.
/// </summary>
sealed file class TokenProvider(string accessToken) : IAuthenticationProvider
{
    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return Task.CompletedTask;
    }
}

/// <summary>
///     Integration tests for FolderTreeService using real Graph API calls.
/// </summary>
/// <remarks>
///     These tests require an authenticated OneDrive account and will make real API calls.
///     They are skipped by default and should be run manually during development.
///     To run: Remove [Fact(Skip = "...")] and replace with [Fact].
/// </remarks>
public class FolderTreeServiceIntegrationShould
{
    private static AuthConfiguration LoadTestConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .AddUserSecrets<FolderTreeServiceIntegrationShould>(true)
            .Build();

        return AuthConfiguration.LoadFromConfiguration(configuration);
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GetRootFoldersFromRealOneDriveAccount()
    {
        // Arrange
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.AccountId is null) throw new InvalidOperationException("Failed to authenticate with OneDrive");

        var graphApiClient = new GraphApiClient(authService, null!, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        // Act
        IReadOnlyList<OneDriveFolderNode> folders = await service.GetRootFoldersAsync(loginResult.AccountId, TestContext.Current.CancellationToken);

        // Assert
        folders.ShouldNotBeEmpty();
        folders.All(f => f.IsFolder).ShouldBeTrue();
        folders.All(f => !string.IsNullOrEmpty(f.Id)).ShouldBeTrue();
        folders.All(f => !string.IsNullOrEmpty(f.Name)).ShouldBeTrue();
        folders.All(f => f.Path.StartsWith('/')).ShouldBeTrue();
        folders.All(f => f.ParentId == null).ShouldBeTrue();
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GetChildFoldersFromRealOneDriveFolder()
    {
        // Arrange
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.AccountId is null) throw new InvalidOperationException("Failed to authenticate with OneDrive");

        var graphApiClient = new GraphApiClient(authService, null!, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        // First get root folders to find one with children
        IReadOnlyList<OneDriveFolderNode> rootFolders = await service.GetRootFoldersAsync(loginResult.AccountId, TestContext.Current.CancellationToken);
        rootFolders.ShouldNotBeEmpty();

        OneDriveFolderNode parentFolder = rootFolders[0];

        // Act
        IReadOnlyList<OneDriveFolderNode> childFolders = await service.GetChildFoldersAsync(loginResult.AccountId, parentFolder.Id, Arg.Any<bool?>(), TestContext.Current.CancellationToken);

        // Assert - may be empty if folder has no subfolders, but should succeed
        _ = childFolders.ShouldNotBeNull();
        if(childFolders.Count > 0)
        {
            childFolders.All(f => f.IsFolder).ShouldBeTrue();
            childFolders.All(f => f.ParentId == parentFolder.Id).ShouldBeTrue();
            childFolders.All(f => !string.IsNullOrEmpty(f.Id)).ShouldBeTrue();
            childFolders.All(f => !string.IsNullOrEmpty(f.Name)).ShouldBeTrue();
        }
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GetFolderHierarchyWithLimitedDepth()
    {
        // Arrange
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.AccountId is null) throw new InvalidOperationException("Failed to authenticate with OneDrive");

        var graphApiClient = new GraphApiClient(authService, null!, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        // Act - limit to depth of 2 to avoid long load times
        IReadOnlyList<OneDriveFolderNode> hierarchy = await service.GetFolderHierarchyAsync(loginResult.AccountId, 2, TestContext.Current.CancellationToken);

        // Assert
        hierarchy.ShouldNotBeEmpty();
        hierarchy.All(f => f.IsFolder).ShouldBeTrue();

        // Check that ChildrenLoaded flag is set on folders that were loaded
        foreach(OneDriveFolderNode folder in hierarchy)
        {
            folder.ChildrenLoaded.ShouldBeTrue();

            // If there are children, verify they're also properly loaded
            if(folder.Children.Count > 0)
            {
                folder.Children.All(c => c.IsFolder).ShouldBeTrue();
                folder.Children.All(c => c.ParentId == folder.Id).ShouldBeTrue();
            }
        }
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task HandleEmptyFoldersGracefully()
    {
        // Arrange
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.AccountId is null) throw new InvalidOperationException("Failed to authenticate with OneDrive");

        _ = await authService.GetAccessTokenAsync(loginResult.AccountId, TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Failed to get access token");

        var graphApiClient = new GraphApiClient(authService, null!, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        // Get root folders
        IReadOnlyList<OneDriveFolderNode> rootFolders = await service.GetRootFoldersAsync(loginResult.AccountId, TestContext.Current.CancellationToken);
        rootFolders.ShouldNotBeEmpty();

        // Act - try to get children from each root folder
        foreach(OneDriveFolderNode folder in rootFolders)
        {
            IReadOnlyList<OneDriveFolderNode> children = await service.GetChildFoldersAsync(loginResult.AccountId, folder.Id, Arg.Any<bool?>(), TestContext.Current.CancellationToken);

            // Assert - should not throw, even if empty
            _ = children.ShouldNotBeNull();
        }
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GraphApiClientCanAccessDrive()
    {
        // Arrange
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.AccountId is null) throw new InvalidOperationException("Failed to authenticate with OneDrive");

        _ = await authService.GetAccessTokenAsync(loginResult.AccountId, TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Failed to get access token");

        var graphApiClient = new GraphApiClient(authService, null!, null!, null!);

        // Act
        Drive? drive = await graphApiClient.GetMyDriveAsync(loginResult.AccountId, TestContext.Current.CancellationToken);
        DriveItem? root = await graphApiClient.GetDriveRootAsync(loginResult.AccountId, TestContext.Current.CancellationToken);
        IEnumerable<DriveItem> rootChildren = await graphApiClient.GetRootChildrenAsync(loginResult.AccountId, TestContext.Current.CancellationToken);

        // Assert
        _ = drive.ShouldNotBeNull();
        drive.Id.ShouldNotBeNullOrEmpty();

        _ = root.ShouldNotBeNull();
        root.Id.ShouldNotBeNullOrEmpty();

        _ = rootChildren.ShouldNotBeNull();
    }
}
