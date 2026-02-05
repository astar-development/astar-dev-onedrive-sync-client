using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Repositories;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using DeltaChangeType = AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models.ChangeType;
using DeltaChangeModel = AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models.DeltaChange;

namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Services;

/// <summary>
/// Service for detecting remote changes using Microsoft Graph API delta queries.
/// </summary>
public class DeltaSyncService(IGraphServiceClientFactory graphFactory, IDeltaTokenRepository deltaTokenRepository) : IDeltaSyncService
{
    private readonly IGraphServiceClientFactory _graphFactory = graphFactory ?? throw new ArgumentNullException(nameof(graphFactory));
    private readonly IDeltaTokenRepository _deltaTokenRepository = deltaTokenRepository ?? throw new ArgumentNullException(nameof(deltaTokenRepository));

    /// <inheritdoc />
    public async Task<DeltaSyncResult> GetDeltaChangesAsync(string accessToken, string hashedAccountId, string driveName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(driveName);

        GraphServiceClient client = _graphFactory.CreateClient(accessToken);
        DeltaToken? savedToken = await _deltaTokenRepository.GetByAccountAndDriveAsync(hashedAccountId, driveName);
        
        var changes = new List<DeltaChangeModel>();
        string? newDeltaToken = null;

        var deltaUrl = !string.IsNullOrWhiteSpace(savedToken?.Token)
            ? savedToken.Token
            : driveName.Equals("root", StringComparison.OrdinalIgnoreCase)
                ? "/me/drive/root/delta"
                : $"/me/drive/items/{driveName}/delta";

        DeltaItemCollectionResponse? deltaResponse = await ExecuteDeltaQueryAsync(client, deltaUrl, cancellationToken);
        
        if (deltaResponse?.Value is not null)
        {
            foreach (DriveItem? item in deltaResponse.Value)
            {
                if (item is not null)
                {
                    changes.Add(ParseDriveItem(item));
                }
            }
        }

        var nextLink = deltaResponse?.OdataNextLink;
        while (!string.IsNullOrWhiteSpace(nextLink))
        {
            deltaResponse = await ExecuteDeltaQueryAsync(client, nextLink, cancellationToken);
            
            if (deltaResponse?.Value is not null)
            {
                foreach (DriveItem? item in deltaResponse.Value)
                {
                    if (item is not null)
                    {
                        changes.Add(ParseDriveItem(item));
                    }
                }
            }

            nextLink = deltaResponse?.OdataNextLink;
        }

        newDeltaToken = deltaResponse?.OdataDeltaLink;

        if (!string.IsNullOrWhiteSpace(newDeltaToken))
        {
            var tokenToSave = new DeltaToken
            {
                Id = savedToken?.Id ?? Guid.NewGuid().ToString(),
                HashedAccountId = hashedAccountId,
                DriveName = driveName,
                Token = newDeltaToken,
                LastSyncAt = DateTime.UtcNow
            };

            await _deltaTokenRepository.SaveAsync(tokenToSave);
        }

        return new DeltaSyncResult
        {
            Changes = changes.AsReadOnly(),
            DeltaToken = newDeltaToken
        };
    }

    private static async Task<DeltaItemCollectionResponse?> ExecuteDeltaQueryAsync(GraphServiceClient client, string deltaUrl, CancellationToken cancellationToken)
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = deltaUrl
        };

        if (!deltaUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = client.RequestAdapter.BaseUrl ?? "https://graph.microsoft.com/v1.0";
            requestInfo.URI = new Uri(baseUrl.TrimEnd('/') + "/" + deltaUrl.TrimStart('/'));
        }
        else
        {
            requestInfo.URI = new Uri(deltaUrl);
        }

        return await client.RequestAdapter.SendAsync(requestInfo, DeltaItemCollectionResponse.CreateFromDiscriminatorValue, default, cancellationToken);
    }

    private static DeltaChangeModel ParseDriveItem(DriveItem item)
    {
        DeltaChangeType changeType = item.Deleted is not null
            ? DeltaChangeType.Deleted
            : item.File is not null || item.Folder is not null
                ? DeltaChangeType.Modified
                : DeltaChangeType.Added;

        return new DeltaChangeModel
        {
            DriveItemId = item.Id ?? string.Empty,
            Name = item.Name ?? string.Empty,
            Path = item.ParentReference?.Path,
            IsFolder = item.Folder is not null,
            ChangeType = changeType,
            RemoteModifiedAt = item.LastModifiedDateTime?.DateTime,
            RemoteHash = item.File?.Hashes?.QuickXorHash
        };
    }
}

/// <summary>
/// Response model for delta query results from Microsoft Graph API.
/// </summary>
internal class DeltaItemCollectionResponse : IParsable
{
    /// <summary>
    /// Gets or sets the collection of drive items.
    /// </summary>
    public List<DriveItem>? Value { get; set; }

    /// <summary>
    /// Gets or sets the URL for the next page of results.
    /// </summary>
    public string? OdataNextLink { get; set; }

    /// <summary>
    /// Gets or sets the delta link for tracking changes.
    /// </summary>
    public string? OdataDeltaLink { get; set; }

    /// <inheritdoc />
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() => new Dictionary<string, Action<IParseNode>>
    {
        { "value", n => Value = n.GetCollectionOfObjectValues(DriveItem.CreateFromDiscriminatorValue)?.ToList() },
        { "@odata.nextLink", n => OdataNextLink = n.GetStringValue() },
        { "@odata.deltaLink", n => OdataDeltaLink = n.GetStringValue() }
    };

    /// <inheritdoc />
    public void Serialize(ISerializationWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteCollectionOfObjectValues("value", Value);
        writer.WriteStringValue("@odata.nextLink", OdataNextLink);
        writer.WriteStringValue("@odata.deltaLink", OdataDeltaLink);
    }

    /// <summary>
    /// Factory method for creating instances from discriminator values.
    /// </summary>
    public static DeltaItemCollectionResponse CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        ArgumentNullException.ThrowIfNull(parseNode);
        return new DeltaItemCollectionResponse();
    }
}
