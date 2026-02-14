namespace AStar.Dev.OneDrive.Sync.Client;

public readonly record struct ApplicationName(string Name)
{
    public static implicit operator string(ApplicationName appName) => appName.Name;
}
