namespace Orchi.Api.Infrastructure.Agents.Orchestration;

using Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;
using Orchi.Api.Infrastructure.Agents.Plans;

public sealed record OrchestrationStepContext(
    Guid ParentChatId,
    ChatSession ParentChat,
    ChatSession CompletedChat,
    string? CompletedPlanId,
    bool Succeeded,
    OrchestrationWorkflowRecord? Workflow,
    IReadOnlyList<PlanMarkdownParser.ParsedPlan> Plans,
    IReadOnlyList<ChatSession> ChildChats);

public enum OrchestrationStepActionKind
{
    None,
    PauseWorkflow,
    KickOffReview,
    KickOffNextPlan
}

public sealed record OrchestrationStepAction(
    OrchestrationStepActionKind Kind,
    PlanMarkdownParser.ParsedPlan? Plan = null,
    Guid? ImplementationChildChatId = null);

public sealed record OrchestrationStepResult(IReadOnlyList<OrchestrationStepAction> Actions);

public interface IOrchestrationStepHandler
{
    Task<OrchestrationStepResult?> HandleAsync(
        OrchestrationStepContext context,
        CancellationToken cancellationToken);
}
