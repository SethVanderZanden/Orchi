using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class AgentModeStrategy : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Agent;

    public Result<AgentTurnRequest> PrepareTurn(ChatSession session, string userContent, IPlanStore plans)
    {
        string prepared = $"{ModeInstructions.Agent}\n\n---\n\n{userContent.Trim()}";
        return Result.Success(new AgentTurnRequest(prepared, []));
    }

    public ValueTask OnTurnCompletedAsync(
        ChatSession session,
        AgentCompletedEvent completed,
        IPlanStore plans,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
