using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentAdapter(
    IAgentCliProcessorFactory processorFactory,
    AgentCliTurnProcessor turnProcessor) : IAgentAdapter
{
    public string AgentId => AgentIds.Cursor;

    public IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        CancellationToken cancellationToken)
    {
        IAgentCliProcessorProfile profile = processorFactory.GetProfile(AgentIds.Cursor);
        return turnProcessor.RunTurnAsync(profile, session, prompt, extraCliArgs, cancellationToken);
    }
}
