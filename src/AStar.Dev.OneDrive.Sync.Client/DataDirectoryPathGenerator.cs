using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class DataDirectoryPathGenerator
{
    public static string GetPlatformUserDataDirectory(string email)
        => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            .CombinePath(ApplicationMetadata.ApplicationName, email.Replace('@', '-').Replace('.', '-'));

    public static string GetPlatformDataDirectory()
        => OperatingSystem.IsWindows()
                                ? WindowsDataPath()
                                : LinuxOrMacDataPath();

    private static string WindowsDataPath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationName);

    private static string LinuxOrMacDataPath() => OperatingSystem.IsMacOS()
        ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).CombinePath("Library", "Application Support", ApplicationMetadata.ApplicationName)
        : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).CombinePath(".config", ApplicationMetadata.ApplicationName);
}
