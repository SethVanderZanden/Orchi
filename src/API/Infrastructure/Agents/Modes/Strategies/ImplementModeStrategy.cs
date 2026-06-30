using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class ImplementModeStrategy(PlanManager planManager) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Implement;

    public Result<AgentTurnRequest> PrepareTurn(ChatSession session, string userContent, IPlanStore plans)
    {
        Result validation = planManager.ValidateAttachedPlan(session.AttachedPlanId);
        if (validation.IsFailure)
        {
            return Result.Failure<AgentTurnRequest>(validation.Error);
        }

        Result<string> planContent = planManager.ResolvePlanContent(session.AttachedPlanId!.Value);
        if (planContent.IsFailure)
        {
            return Result.Failure<AgentTurnRequest>(planContent.Error);
        }

        string prepared =
            $"""
            {ModeInstructions.Implement}

            ## Attached plan

            {planContent.Value}

            ---

            {userContent.Trim()}
            """;

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
