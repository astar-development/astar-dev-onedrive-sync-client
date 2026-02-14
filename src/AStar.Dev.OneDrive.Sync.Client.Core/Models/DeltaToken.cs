namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

public sealed record DeltaToken(string AccountId, string Id, string Token, DateTimeOffset LastSyncedUtc);
