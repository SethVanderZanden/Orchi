using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Shared;

public sealed record OrchestrationPlanResponse(string PlanId, string Title, string ContentMarkdown);

public sealed record OrchestrationChildResponse(
    string PlanId,
    Guid ChatId,
    string Mode,
    string? PlanFilePath);

public sealed record OrchestrationStateResponse(
    string Status,
    int? CurrentStep,
    int? TotalSteps,
    string? CurrentPlanId,
    IReadOnlyList<string> SequencePlanIds,
    IReadOnlyList<OrchestrationPlanResponse> Plans,
    IReadOnlyList<OrchestrationChildResponse> Children);

public sealed record OrchestrationWorkflowSsePayload(
    string Status,
    int? CurrentStep,
    int? TotalSteps,
    string? PlanId);

public sealed record OrchestrationChatCreatedSsePayload(
    Guid ChatId,
    string Mode,
    Guid ParentChatId,
    string? PlanId,
    string? PlanFilePath);

public sealed record OrchestrationParentMessageSsePayload(
    Guid MessageId,
    string Role,
    string Content);

public sealed record OrchestrationAgentStatusSsePayload(Guid ChildChatId, string Phase);

public sealed record OrchestrationAgentTokenSsePayload(Guid ChildChatId, string Text);

public sealed record OrchestrationAgentToolSsePayload(Guid ChildChatId, string Label);

public sealed record OrchestrationAgentDoneSsePayload(
    Guid ChildChatId,
    Guid MessageId,
    bool Succeeded);

public sealed record OrchestrationAgentErrorSsePayload(
    Guid ChildChatId,
    string Code,
    string Message);

internal static class OrchestrationMapper
{
    public static OrchestrationStateResponse ToResponse(OrchestrationSnapshot snapshot) =>
        new(
            snapshot.Status,
            snapshot.CurrentStep,
            snapshot.TotalSteps,
            snapshot.CurrentPlanId,
            snapshot.SequencePlanIds,
            snapshot.Plans
                .Select(plan => new OrchestrationPlanResponse(plan.PlanId, plan.Title, plan.ContentMarkdown))
                .ToArray(),
            snapshot.Children
                .Select(child => new OrchestrationChildResponse(
                    child.PlanId,
                    child.ChatId,
                    child.Mode,
                    child.PlanFilePath))
                .ToArray());
}

internal static class OrchestrationSseWriter
{
    public static Task WriteEventAsync(
        OrchestrationEventHub eventHub,
        Stream stream,
        Guid parentChatId,
        OrchestrationEvent orchestrationEvent,
        CancellationToken cancellationToken)
    {
        switch (orchestrationEvent)
        {
            case OrchestrationWorkflowEvent workflow:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "workflow",
                    new OrchestrationWorkflowSsePayload(
                        workflow.Status,
                        workflow.CurrentStep,
                        workflow.TotalSteps,
                        workflow.PlanId),
                    cancellationToken);

            case OrchestrationChatCreatedEvent chatCreated:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "chat_created",
                    new OrchestrationChatCreatedSsePayload(
                        chatCreated.ChatId,
                        chatCreated.Mode,
                        chatCreated.ParentChatId,
                        chatCreated.PlanId,
                        chatCreated.PlanFilePath),
                    cancellationToken);

            case OrchestrationParentMessageEvent parentMessage:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "parent_message",
                    new OrchestrationParentMessageSsePayload(
                        parentMessage.MessageId,
                        parentMessage.Role,
                        parentMessage.Content),
                    cancellationToken);

            case OrchestrationAgentStatusEvent status:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "agent_status",
                    new OrchestrationAgentStatusSsePayload(status.ChildChatId, status.Phase),
                    cancellationToken);

            case OrchestrationAgentTokenEvent token:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "agent_token",
                    new OrchestrationAgentTokenSsePayload(token.ChildChatId, token.Text),
                    cancellationToken);

            case OrchestrationAgentToolEvent tool:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "agent_tool",
                    new OrchestrationAgentToolSsePayload(tool.ChildChatId, tool.Label),
                    cancellationToken);

            case OrchestrationAgentDoneEvent done:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "agent_done",
                    new OrchestrationAgentDoneSsePayload(done.ChildChatId, done.MessageId, done.Succeeded),
                    cancellationToken);

            case OrchestrationAgentErrorEvent error:
                return ChatSseWriter.WriteEventAsync(
                    stream,
                    "agent_error",
                    new OrchestrationAgentErrorSsePayload(error.ChildChatId, error.Code, error.Message),
                    cancellationToken);

            default:
                return Task.CompletedTask;
        }
    }
}
