namespace Orchi.Api.Infrastructure.Agents;

public interface IAgentAdapter
{
    string AgentId { get; }

    IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        CancellationToken cancellationToken);
}
