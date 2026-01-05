using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Services.OneDriveServices;

/// <summary>
/// Wrapper implementation for Microsoft Graph API client.
/// </summary>
/// <remarks>
/// Wraps GraphServiceClient to provide a testable abstraction for OneDrive operations.
/// </remarks>
public sealed class GraphApiClient : IGraphApiClient
{
    private readonly GraphServiceClient _graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphApiClient"/> class.
    /// </summary>
    /// <param name="graphClient">The configured Graph service client.</param>
    public GraphApiClient(GraphServiceClient graphClient)
    {
        ArgumentNullException.ThrowIfNull(graphClient);
        _graphClient = graphClient;
    }

    /// <inheritdoc/>
    public async Task<Drive?> GetMyDriveAsync(CancellationToken cancellationToken = default)
    {
        return await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveRootAsync(CancellationToken cancellationToken = default)
    {
        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Root is null) return null;

        return await _graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await _graphClient.Drives[drive.Id].Items[itemId].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return null;

        return await _graphClient.Drives[drive.Id].Items[itemId].GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetRootChildrenAsync(CancellationToken cancellationToken = default)
    {
        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var root = await _graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
        if (root?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await _graphClient.Drives[drive.Id].Items[root.Id].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }
}
