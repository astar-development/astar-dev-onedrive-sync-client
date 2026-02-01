using System.Text.Json;
using AStar.Dev.OneDrive.Client.Core.Models;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;
public static class SerilogLogParser
{
    public static DebugLogEntry? Parse(string jsonLine, int id)
    {
        if (string.IsNullOrWhiteSpace(jsonLine) || !jsonLine.TrimStart().StartsWith('{'))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            JsonElement root = doc.RootElement;

            // Timestamp
            DateTimeOffset timestamp = root.TryGetProperty("Timestamp", out JsonElement ts)
                ? ts.GetDateTimeOffset()
                : DateTimeOffset.MinValue;

            // Level
            var level = root.TryGetProperty("Level", out JsonElement lvl)
                ? lvl.GetString() ?? "Info"
                : "Info";

            // SourceContext (ILogger<T>)
            var source = root.TryGetProperty("Properties", out JsonElement props) &&
                         props.TryGetProperty("SourceContext", out JsonElement src)
                ? src.GetString() ?? ""
                : "";

            // AccountId (if you enrich it)
            var accountId = props.TryGetProperty("AccountHash", out JsonElement acc)
                ? acc.GetString() ?? ""
                : "";

            // Message
            var message = root.TryGetProperty("RenderedMessage", out JsonElement msg)
                ? msg.GetString() ?? ""
                : root.TryGetProperty("MessageTemplate", out JsonElement tmpl)
                    ? tmpl.GetString() ?? ""
                    : "";

            // Exception
            var exception = root.TryGetProperty("Exception", out JsonElement ex)
                ? ex.GetString()
                : null;

            return new DebugLogEntry(
                Id: id,
                AccountId: accountId,
                Timestamp: timestamp,
                LogLevel: level,
                Source: source,
                Message: message,
                Exception: exception
            );
        }
        catch
        {
            // If a line is corrupted or not JSON, skip it
            return null;
        }
    }
}
