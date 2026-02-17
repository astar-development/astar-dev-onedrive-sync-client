using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

public static class SerilogLogParser
{
    public static DebugLogEntry? Parse(string jsonLine, int id)
    {
        if(string.IsNullOrWhiteSpace(jsonLine) || !jsonLine.TrimStart().StartsWith('{'))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            JsonElement root = doc.RootElement;

            DateTimeOffset timestamp = root.TryGetProperty("Timestamp", out JsonElement ts)
                ? ts.GetDateTimeOffset()
                : DateTimeOffset.MinValue;

            var level = root.TryGetProperty("Level", out JsonElement lvl)
                ? lvl.GetString() ?? "Info"
                : "Info";

            var source = root.TryGetProperty("Properties", out JsonElement props) &&
                         props.TryGetProperty("SourceContext", out JsonElement src)
                ? src.GetString() ?? ""
                : "";

            var accountId = props.TryGetProperty("AccountHash", out JsonElement acc)
                ? acc.GetString() ?? ""
                : "";

            var message = root.TryGetProperty("RenderedMessage", out JsonElement msg)
                ? msg.GetString() ?? ""
                : root.TryGetProperty("MessageTemplate", out JsonElement tmpl)
                    ? tmpl.GetString() ?? ""
                    : "";

            var exception = root.TryGetProperty("Exception", out JsonElement ex)
                ? ex.GetString()
                : null;

            return new DebugLogEntry(id, new HashedAccountId(accountId), timestamp, level, source, message, exception);
        }
        catch
        {
            return null;
        }
    }
}
