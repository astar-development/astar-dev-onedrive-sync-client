namespace AStar.Dev.OneDrive.Client;

public readonly partial record struct ApplicationName(string Name)
{
    public static implicit operator string(ApplicationName appName) => appName.Name;
}
