using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Orchestration.Handlers;

public sealed class SequentialAdvanceStepHandler : IOrchestrationStepHandler
{
    public Task<OrchestrationStepResult?> HandleAsync(
        OrchestrationStepContext context,
        CancellationToken cancellationToken)
    {
        if (context.Workflow is null)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        if (!string.Equals(context.CompletedChat.Mode, ImplementationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        if (context.Workflow.SequencePlanIds.Count == 0)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        if (!context.Succeeded)
        {
            return Task.FromResult<OrchestrationStepResult?>(
                new OrchestrationStepResult([
                    new OrchestrationStepAction(OrchestrationStepActionKind.PauseWorkflow)
                ]));
        }

        if (!string.Equals(context.Workflow.Status, OrchestrationWorkflowStatus.Running, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        if (context.CompletedPlanId is null)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        int completedIndex = IndexOfPlanId(context.Workflow.SequencePlanIds, context.CompletedPlanId);

        if (completedIndex < 0)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        int expectedIndex = context.Workflow.NextSequenceIndex - 1;
        if (completedIndex != expectedIndex)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        for (int index = context.Workflow.NextSequenceIndex; index < context.Workflow.SequencePlanIds.Count; index++)
        {
            string nextPlanId = context.Workflow.SequencePlanIds[index];
            if (HasImplementationChild(nextPlanId, context.ChildChats))
            {
                continue;
            }

            PlanMarkdownParser.ParsedPlan? nextPlan = context.Plans
                .FirstOrDefault(plan => string.Equals(plan.PlanId, nextPlanId, StringComparison.OrdinalIgnoreCase));

            if (nextPlan is null)
            {
                continue;
            }

            return Task.FromResult<OrchestrationStepResult?>(
                new OrchestrationStepResult([
                    new OrchestrationStepAction(OrchestrationStepActionKind.KickOffNextPlan, nextPlan)
                ]));
        }

        return Task.FromResult<OrchestrationStepResult?>(null);
    }

    private static bool HasImplementationChild(string planId, IReadOnlyList<ChatSession> childChats)
    {
        return childChats.Any(chat =>
            string.Equals(chat.Mode, ImplementationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(PlanMarkdownParser.TryExtractPlanIdFromPath(chat.PlanFilePath), planId, StringComparison.OrdinalIgnoreCase));
    }

    private static int IndexOfPlanId(IReadOnlyList<string> sequencePlanIds, string planId)
    {
        for (int index = 0; index < sequencePlanIds.Count; index++)
        {
            if (string.Equals(sequencePlanIds[index], planId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}
