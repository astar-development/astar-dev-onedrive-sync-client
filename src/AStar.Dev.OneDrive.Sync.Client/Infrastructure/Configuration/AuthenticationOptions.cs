namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;

public record AuthenticationOptions
{
    public const string SectionName = "Authentication";
    
    public MicrosoftOptions Microsoft { get; init; } = new();
}

public record MicrosoftOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = [];
    public int LoginTimeout { get; init; } = 30;
    public int TokenRefreshMargin { get; init; } = 5;
    public string? ClientSecret { get; init; }
}
