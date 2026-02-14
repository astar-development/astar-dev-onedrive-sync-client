using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.OneDriveServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services.OneDriveServices;

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
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.HashedAccountId is null)
            throw new InvalidOperationException("Failed to authenticate with OneDrive");

        var graphApiClient = new GraphApiClient(authService, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        IReadOnlyList<OneDriveFolderNode> folders = await service.GetRootFoldersAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken);

        folders.ShouldNotBeEmpty();
        folders.All(f => f.IsFolder).ShouldBeTrue();
        folders.All(f => !string.IsNullOrEmpty(f.DriveItemId)).ShouldBeTrue();
        folders.All(f => !string.IsNullOrEmpty(f.Name)).ShouldBeTrue();
        folders.All(f => f.Path.StartsWith('/')).ShouldBeTrue();
        folders.All(f => f.ParentId == null).ShouldBeTrue();
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GetChildFoldersFromRealOneDriveFolder()
    {
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.HashedAccountId is null)
            throw new InvalidOperationException("Failed to authenticate with OneDrive");

        var graphApiClient = new GraphApiClient(authService, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        IReadOnlyList<OneDriveFolderNode> rootFolders = await service.GetRootFoldersAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken);
        rootFolders.ShouldNotBeEmpty();

        OneDriveFolderNode parentFolder = rootFolders[0];

        IReadOnlyList<OneDriveFolderNode> childFolders = await service.GetChildFoldersAsync(loginResult.HashedAccountId, parentFolder.DriveItemId, Arg.Any<bool?>(), TestContext.Current.CancellationToken);

        _ = childFolders.ShouldNotBeNull();
        if(childFolders.Count > 0)
        {
            childFolders.All(f => f.IsFolder).ShouldBeTrue();
            childFolders.All(f => f.ParentId == parentFolder.DriveItemId).ShouldBeTrue();
            childFolders.All(f => !string.IsNullOrEmpty(f.DriveItemId)).ShouldBeTrue();
            childFolders.All(f => !string.IsNullOrEmpty(f.Name)).ShouldBeTrue();
        }
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GetFolderHierarchyWithLimitedDepth()
    {
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.HashedAccountId is null)
            throw new InvalidOperationException("Failed to authenticate with OneDrive");

        var graphApiClient = new GraphApiClient(authService, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        IReadOnlyList<OneDriveFolderNode> hierarchy = await service.GetFolderHierarchyAsync(loginResult.HashedAccountId, 2, TestContext.Current.CancellationToken);

        hierarchy.ShouldNotBeEmpty();
        hierarchy.All(f => f.IsFolder).ShouldBeTrue();

        foreach(OneDriveFolderNode folder in hierarchy)
        {
            folder.ChildrenLoaded.ShouldBeTrue();

            if(folder.Children.Count > 0)
            {
                folder.Children.All(c => c.IsFolder).ShouldBeTrue();
                folder.Children.All(c => c.ParentId == folder.DriveItemId).ShouldBeTrue();
            }
        }
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task HandleEmptyFoldersGracefully()
    {
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.HashedAccountId is null)
            throw new InvalidOperationException("Failed to authenticate with OneDrive");

        _ = await authService.GetAccessTokenAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Failed to get access token");

        var graphApiClient = new GraphApiClient(authService, null!, null!);
        var service = new FolderTreeService(graphApiClient, authService, null!);

        // Get root folders
        IReadOnlyList<OneDriveFolderNode> rootFolders = await service.GetRootFoldersAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken);
        rootFolders.ShouldNotBeEmpty();

        foreach(OneDriveFolderNode folder in rootFolders)
        {
            IReadOnlyList<OneDriveFolderNode> children = await service.GetChildFoldersAsync(loginResult.HashedAccountId, folder.DriveItemId, Arg.Any<bool?>(), TestContext.Current.CancellationToken);
            _ = children.ShouldNotBeNull();
        }
    }

    [Fact(Skip = "Integration test - requires real OneDrive account authentication")]
    public async Task GraphApiClientCanAccessDrive()
    {
        AuthConfiguration config = LoadTestConfiguration();
        AuthService authService = await AuthService.CreateAsync(config);
        AuthenticationResult loginResult = await authService.LoginAsync(TestContext.Current.CancellationToken);

        if(!loginResult.Success || loginResult.HashedAccountId is null)
            throw new InvalidOperationException("Failed to authenticate with OneDrive");

        _ = await authService.GetAccessTokenAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Failed to get access token");

        var graphApiClient = new GraphApiClient(authService, null!, null!);

        Drive? drive = await graphApiClient.GetMyDriveAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken);
        DriveItem? root = await graphApiClient.GetDriveRootAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken);
        IEnumerable<DriveItem> rootChildren = await graphApiClient.GetRootChildrenAsync(loginResult.HashedAccountId, TestContext.Current.CancellationToken);

        _ = drive.ShouldNotBeNull();
        drive.Id.ShouldNotBeNullOrEmpty();

        _ = root.ShouldNotBeNull();
        root.Id.ShouldNotBeNullOrEmpty();

        _ = rootChildren.ShouldNotBeNull();
    }
}
