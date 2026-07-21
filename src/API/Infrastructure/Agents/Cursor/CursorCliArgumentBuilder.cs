using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorCliArgumentBuilder(IOptions<CursorAgentOptions> options) : IAgentCliArgumentBuilder
{
    public IReadOnlyList<string> BuildArguments(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        string? entryScript)
    {
        CursorAgentOptions config = options.Value;
        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(entryScript))
        {
            arguments.Add(entryScript);
        }

        foreach (string defaultArg in config.DefaultArgs.Distinct(StringComparer.Ordinal))
        {
            arguments.Add(defaultArg);
        }

        arguments.Add("-p");
        arguments.Add("--output-format");
        arguments.Add("stream-json");
        arguments.Add("--stream-partial-output");
        arguments.Add("--workspace");
        arguments.Add(session.WorkspacePath);

        if (!string.IsNullOrWhiteSpace(session.ModelId))
        {
            arguments.Add("--model");
            arguments.Add(session.ModelId);
        }

        AppendNonWhiteSpaceArgs(arguments, extraCliArgs);
        AppendResumeArgs(arguments, session);
        arguments.Add(prompt);
        return arguments;
    }

    private static void AppendNonWhiteSpaceArgs(List<string> arguments, IReadOnlyList<string> extraCliArgs)
    {
        foreach (string extraArg in extraCliArgs)
        {
            if (string.IsNullOrWhiteSpace(extraArg))
            {
                continue;
            }

            arguments.Add(extraArg);
        }
    }

    private static void AppendResumeArgs(List<string> arguments, ChatSession session)
    {
        if (string.IsNullOrWhiteSpace(session.ExternalSessionId))
        {
            return;
        }

        arguments.Add("--resume");
        arguments.Add(session.ExternalSessionId);
    }
}
