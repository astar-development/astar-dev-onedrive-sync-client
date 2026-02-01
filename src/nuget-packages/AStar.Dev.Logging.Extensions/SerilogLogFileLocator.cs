namespace AStar.Dev.Logging.Extensions.Serilog;

/// <summary>
///  Locates Serilog log files in a specified directory.
/// </summary>
public class SerilogLogFileLocator
{
    /// <summary>
    ///   Gets the latest log file in the specified directory.
    /// </summary>
    /// <param name="logDir"></param>
    /// <returns></returns>
    public static string? GetLatestLogFile(string logDir)
    {
        var files = Directory.GetFiles(logDir, "sync*.log");

        return files.Length == 0
            ? null
            : files
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    /// <summary>
    ///  Gets all log files in the specified directory ordered by last write time descending.
    /// </summary>
    /// <param name="logDir"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetAllLogFiles(string logDir)
        => Directory.GetFiles(logDir, "sync*.log")
            .OrderByDescending(File.GetLastWriteTimeUtc);
}