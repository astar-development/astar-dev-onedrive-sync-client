namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

public record MsalConfigurationSettings(string ClientId, string RedirectUri, string GraphUri, string[] Scopes, string CachePrefix);
