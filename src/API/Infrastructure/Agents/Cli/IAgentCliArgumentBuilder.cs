namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Strategy: builds argv for one agent turn from session state and the user prompt.
/// </summary>
public interface IAgentCliArgumentBuilder
{
    IReadOnlyList<string> BuildArguments(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        string? entryScript);
}
