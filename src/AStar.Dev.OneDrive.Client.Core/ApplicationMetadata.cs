namespace AStar.Dev.OneDrive.Client.Core;

/// <summary>
///  Contains metadata related to the application. This class provides a centralized location for storing constants and other relevant information about the application, such as its name and version. By using a dedicated class for this purpose, we can ensure that any references to the application's metadata are consistent throughout the codebase and can be easily updated if necessary.
/// </summary>
public static class ApplicationMetadata
{
    /// <summary>
    /// The name of the application. This constant can be used throughout the codebase whenever we need to reference the application's name, ensuring consistency and making it easier to update if the name ever needs to change.
    /// </summary>
    public const string ApplicationName = "AStar.Dev.OneDrive.Sync.Client";

    /// <summary>
    /// The folder name to use for storing application data, such as the authentication token cache. This is derived from the application name by replacing dots with hyphens and converting to lowercase, resulting in a folder name that is suitable for use in file paths across different operating systems. By using a consistent folder name based on the application name, we can ensure that all application data is stored in a predictable location, making it easier to manage and maintain.
    /// </summary>
    public static string ApplicationFolder => ApplicationName.Replace('.', '-').ToLower(); // e.g. "astar-dev-onedrive-sync-client"

    /// <summary>
    /// The version of the application. This constant can be used throughout the codebase whenever we need to reference the application's version, ensuring consistency and making it easier to update if the version ever needs to change.
    /// </summary>
    public const string ApplicationVersion = "1.0.0";
}
