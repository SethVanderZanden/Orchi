namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public abstract record OrchestrationEvent;

public sealed record OrchestrationWorkflowEvent(
    string Status,
    int? CurrentStep,
    int? TotalSteps,
    string? PlanId) : OrchestrationEvent;

public sealed record OrchestrationChatCreatedEvent(
    Guid ChatId,
    string Mode,
    Guid ParentChatId,
    string? PlanId,
    string? PlanFilePath) : OrchestrationEvent;

public sealed record OrchestrationParentMessageEvent(
    Guid MessageId,
    string Role,
    string Content) : OrchestrationEvent;

public sealed record OrchestrationAgentStatusEvent(Guid ChildChatId, string Phase) : OrchestrationEvent;

public sealed record OrchestrationAgentTokenEvent(Guid ChildChatId, string Text) : OrchestrationEvent;

public sealed record OrchestrationAgentToolEvent(Guid ChildChatId, string Label) : OrchestrationEvent;

public sealed record OrchestrationAgentDoneEvent(
    Guid ChildChatId,
    Guid MessageId,
    bool Succeeded) : OrchestrationEvent;

public sealed record OrchestrationAgentErrorEvent(
    Guid ChildChatId,
    string Code,
    string Message) : OrchestrationEvent;
