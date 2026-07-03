using System.Text;
using System.Text.Json;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorAgentDebugLog
{
    private const string SessionId = "b60e6f";
    private const string LogFileName = "debug-b60e6f.log";

    internal static void Write(
        string workspacePath,
        string hypothesisId,
        string location,
        string message,
        object data)
    {
        // #region agent log
        try
        {
            string logPath = Path.Combine(workspacePath, LogFileName);
            var payload = new Dictionary<string, object?>
            {
                ["sessionId"] = SessionId,
                ["hypothesisId"] = hypothesisId,
                ["location"] = location,
                ["message"] = message,
                ["data"] = data,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            File.AppendAllText(logPath, JsonSerializer.Serialize(payload) + Environment.NewLine);
        }
        catch
        {
            // ignore debug logging failures
        }
        // #endregion
    }

    internal static string BuildCopyPasteCommand(string executablePath, IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder();
        builder.Append(QuoteForCmd(executablePath));

        foreach (string argument in arguments)
        {
            builder.Append(' ');
            builder.Append(QuoteForCmd(argument));
        }

        return builder.ToString();
    }

    internal static void WriteWorkspaceDiagnostics(string workspacePath, Guid chatId, string prompt, string copyPasteCommand)
    {
        try
        {
            string directory = Path.Combine(workspacePath, ".orchi", "debug");
            Directory.CreateDirectory(directory);

            string prefix = Path.Combine(directory, $"chat-{chatId:N}");
            File.WriteAllText($"{prefix}-prompt.xml", prompt, Encoding.UTF8);
            File.WriteAllText($"{prefix}-command.cmd", copyPasteCommand, Encoding.UTF8);
        }
        catch
        {
            // ignore diagnostic file failures
        }
    }

    private static string QuoteForCmd(string value)
    {
        if (!value.Contains('"') && !value.Contains(' ') && !value.Contains('\t'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
