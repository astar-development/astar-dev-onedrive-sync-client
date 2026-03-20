using System.Reflection;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class ApplicationMetadata
{
    public const string ApplicationName = "AStar.Dev.OneDrive.Sync";
    
    public static string ApplicationVersion => $"V{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}";
    
    public  const string DatabaseFileName = "onedrivesync.db";
}
