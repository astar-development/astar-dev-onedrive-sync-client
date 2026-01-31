namespace AStar.Dev.OneDrive.Client.Core.Models;

public sealed record DeltaToken(string AccountId, string Id, string Token, DateTimeOffset LastSyncedUtc);
