using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class PlanModeStrategy : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Plan;

    public Result<AgentTurnRequest> PrepareTurn(ChatSession session, string userContent, IPlanStore plans)
    {
        string prepared = $"{ModeInstructions.Plan}\n\n---\n\n{userContent.Trim()}";
        return Result.Success(new AgentTurnRequest(prepared, ["--mode=plan"]));
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
