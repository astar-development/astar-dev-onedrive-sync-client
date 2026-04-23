using System;
using System.IO;

namespace AStar.Dev.Utilities;

/// <summary>
///
/// </summary>
public static class ApplicationPathsProvider
{
    /// <param name="applicationName"></param>
#pragma warning disable CA1034
    extension(string applicationName)
#pragma warning restore CA1034
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string ApplicationDirectory() => GetPlatformDataDirectory(applicationName);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string LogsDirectory() => ResolveLogDirectory(applicationName);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string UserDirectory() => ResolveUsersDirectory(applicationName);
    }

    private static string ResolveLogDirectory(string applicationName)
    {
        var logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath(applicationName, "logs");

        _ = Directory.CreateDirectory(logDirectory);

        return logDirectory;
    }

    private static string ResolveUsersDirectory(string applicationName)
    {
        var logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath(applicationName, "sync");

        _ = Directory.CreateDirectory(logDirectory);

        return logDirectory;
    }

    private static string GetPlatformDataDirectory(string applicationName)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var directory= OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                applicationName)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support", applicationName)
                : Path.Combine(home, ".config", applicationName);
        _ = Directory.CreateDirectory(directory);

        return directory;
    }
}
