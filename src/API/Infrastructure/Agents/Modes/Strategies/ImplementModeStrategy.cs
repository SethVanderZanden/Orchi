using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class ImplementModeStrategy(
    PlanManager planManager,
    AgentPromptComposer promptComposer) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Implement;

    public async ValueTask<Result<AgentTurnRequest>> PrepareTurnAsync(
        ChatSession session,
        string userContent,
        IPlanStore plans,
        CancellationToken cancellationToken)
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

        string planSection = $"## Attached plan\n\n{planContent.Value}";
        return await promptComposer.ComposeAsync(
            session,
            userContent,
            ModeInstructions.Implement,
            planSection,
            cancellationToken);
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
