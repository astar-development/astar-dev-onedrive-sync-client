using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Services.OneDriveServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Diagnostics.CodeAnalysis;

namespace AStarOneDriveClient.Tests.Unit.Services.OneDriveServices;

/// <summary>
/// Helper class for providing bearer token authentication to GraphServiceClient.
/// </summary>
file sealed class TokenProvider(string accessToken) : IAuthenticationProvider
{
    public Task AuthenticateRequestAsync(
        Microsoft.Kiota.Abstractions.RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Integration tests for FolderTreeService using real Graph API calls.
/// </summary>
/// <remarks>
/// These tests require an authenticated OneDrive account and will make real API calls.
/// They are skipped by default and should be run manually during development.
/// To run: Remove [Fact(Skip = "...")] and replace with [Fact].
/// </remarks>
[SuppressMessage("xUnit1004", "xUnit1004:TestMethodShouldNotBeSkipped", Justification = "Integration tests require manual setup with authenticated OneDrive account")]
public class FolderTreeServiceIntegrationShould
{
    private static AuthConfiguration LoadTestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<FolderTreeServiceIntegrationShould>(optional: true)
            .Build();

        return AuthConfiguration.LoadFromConfiguration(configuration);
    }

    [Fact(Skip = "Integration test - requires authenticated OneDrive account")]
    public async Task GetRootFoldersFromRealOneDriveAccount()
    {
        // Arrange
        var config = LoadTestConfiguration();
        var authService = await AuthService.CreateAsync(config);
        var loginResult = await authService.LoginAsync();

        if (!loginResult.Success || loginResult.AccountId is null)
        {
            throw new InvalidOperationException("Failed to authenticate with OneDrive");
        }

        var accessToken = await authService.GetAccessTokenAsync(loginResult.AccountId);
        if (accessToken is null)
        {
            throw new InvalidOperationException("Failed to get access token");
        }

        var graphApiClient = new GraphApiClient(authService);
        var service = new FolderTreeService(graphApiClient, authService);

        // Act
        var folders = await service.GetRootFoldersAsync(loginResult.AccountId);

        // Assert
        folders.ShouldNotBeEmpty();
        folders.All(f => f.IsFolder).ShouldBeTrue();
        folders.All(f => !string.IsNullOrEmpty(f.Id)).ShouldBeTrue();
        folders.All(f => !string.IsNullOrEmpty(f.Name)).ShouldBeTrue();
        folders.All(f => f.Path.StartsWith('/')).ShouldBeTrue();
        folders.All(f => f.ParentId == null).ShouldBeTrue();
    }

    [Fact(Skip = "Integration test - requires authenticated OneDrive account")]
    public async Task GetChildFoldersFromRealOneDriveFolder()
    {
        // Arrange
        var config = LoadTestConfiguration();
        var authService = await AuthService.CreateAsync(config);
        var loginResult = await authService.LoginAsync();

        if (!loginResult.Success || loginResult.AccountId is null)
        {
            throw new InvalidOperationException("Failed to authenticate with OneDrive");
        }

        var accessToken = await authService.GetAccessTokenAsync(loginResult.AccountId);
        if (accessToken is null)
        {
            throw new InvalidOperationException("Failed to get access token");
        }

        var graphApiClient = new GraphApiClient(authService);
        var service = new FolderTreeService(graphApiClient, authService);

        // First get root folders to find one with children
        var rootFolders = await service.GetRootFoldersAsync(loginResult.AccountId);
        rootFolders.ShouldNotBeEmpty();

        var parentFolder = rootFolders[0];

        // Act
        var childFolders = await service.GetChildFoldersAsync(loginResult.AccountId, parentFolder.Id);

        // Assert - may be empty if folder has no subfolders, but should succeed
        childFolders.ShouldNotBeNull();
        if (childFolders.Count > 0)
        {
            childFolders.All(f => f.IsFolder).ShouldBeTrue();
            childFolders.All(f => f.ParentId == parentFolder.Id).ShouldBeTrue();
            childFolders.All(f => !string.IsNullOrEmpty(f.Id)).ShouldBeTrue();
            childFolders.All(f => !string.IsNullOrEmpty(f.Name)).ShouldBeTrue();
        }
    }

    [Fact(Skip = "Integration test - requires authenticated OneDrive account")]
    public async Task GetFolderHierarchyWithLimitedDepth()
    {
        // Arrange
        var config = LoadTestConfiguration();
        var authService = await AuthService.CreateAsync(config);
        var loginResult = await authService.LoginAsync();

        if (!loginResult.Success || loginResult.AccountId is null)
        {
            throw new InvalidOperationException("Failed to authenticate with OneDrive");
        }

        var accessToken = await authService.GetAccessTokenAsync(loginResult.AccountId);
        if (accessToken is null)
        {
            throw new InvalidOperationException("Failed to get access token");
        }

        var graphApiClient = new GraphApiClient(authService);
        var service = new FolderTreeService(graphApiClient, authService);

        // Act - limit to depth of 2 to avoid long load times
        var hierarchy = await service.GetFolderHierarchyAsync(loginResult.AccountId, maxDepth: 2);

        // Assert
        hierarchy.ShouldNotBeEmpty();
        hierarchy.All(f => f.IsFolder).ShouldBeTrue();

        // Check that ChildrenLoaded flag is set on folders that were loaded
        foreach (var folder in hierarchy)
        {
            folder.ChildrenLoaded.ShouldBeTrue();

            // If there are children, verify they're also properly loaded
            if (folder.Children.Count > 0)
            {
                folder.Children.All(c => c.IsFolder).ShouldBeTrue();
                folder.Children.All(c => c.ParentId == folder.Id).ShouldBeTrue();
            }
        }
    }

    [Fact(Skip = "Integration test - requires authenticated OneDrive account")]
    public async Task HandleEmptyFoldersGracefully()
    {
        // Arrange
        var config = LoadTestConfiguration();
        var authService = await AuthService.CreateAsync(config);
        var loginResult = await authService.LoginAsync();

        if (!loginResult.Success || loginResult.AccountId is null)
        {
            throw new InvalidOperationException("Failed to authenticate with OneDrive");
        }

        var accessToken = await authService.GetAccessTokenAsync(loginResult.AccountId);
        if (accessToken is null)
        {
            throw new InvalidOperationException("Failed to get access token");
        }

        var graphApiClient = new GraphApiClient(authService);
        var service = new FolderTreeService(graphApiClient, authService);

        // Get root folders
        var rootFolders = await service.GetRootFoldersAsync(loginResult.AccountId);
        rootFolders.ShouldNotBeEmpty();

        // Act - try to get children from each root folder
        foreach (var folder in rootFolders)
        {
            var children = await service.GetChildFoldersAsync(loginResult.AccountId, folder.Id);

            // Assert - should not throw, even if empty
            children.ShouldNotBeNull();
        }
    }

    [Fact(Skip = "Integration test - requires authenticated OneDrive account")]
    public async Task GraphApiClientCanAccessDrive()
    {
        // Arrange
        var config = LoadTestConfiguration();
        var authService = await AuthService.CreateAsync(config);
        var loginResult = await authService.LoginAsync();

        if (!loginResult.Success || loginResult.AccountId is null)
        {
            throw new InvalidOperationException("Failed to authenticate with OneDrive");
        }

        var accessToken = await authService.GetAccessTokenAsync(loginResult.AccountId);
        if (accessToken is null)
        {
            throw new InvalidOperationException("Failed to get access token");
        }

        var graphApiClient = new GraphApiClient(authService);

        // Act
        var drive = await graphApiClient.GetMyDriveAsync(loginResult.AccountId);
        var root = await graphApiClient.GetDriveRootAsync(loginResult.AccountId);
        var rootChildren = await graphApiClient.GetRootChildrenAsync(loginResult.AccountId);

        // Assert
        drive.ShouldNotBeNull();
        drive.Id.ShouldNotBeNullOrEmpty();

        root.ShouldNotBeNull();
        root.Id.ShouldNotBeNullOrEmpty();

        rootChildren.ShouldNotBeNull();
    }
}
