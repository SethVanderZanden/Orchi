using System.Runtime.CompilerServices;
using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexAgentAdapter(
    IAgentCliProcessorFactory processorFactory,
    AgentCliTurnProcessor turnProcessor) : IAgentAdapter
{
    public string AgentId => AgentIds.Codex;

    public IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        CancellationToken cancellationToken)
    {
        IAgentCliProcessorProfile profile = processorFactory.GetProfile(AgentIds.Codex);
        return turnProcessor.RunTurnAsync(profile, session, prompt, extraCliArgs, cancellationToken);
    }
}
