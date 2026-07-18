using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public sealed class OrchestrationAgentRunner(
    AgentSessionManager sessionManager,
    OrchestrationEventHub eventHub,
    ILogger<OrchestrationAgentRunner> logger)
{
    public async Task RunTurnAsync(
        Guid parentChatId,
        Guid childChatId,
        string content,
        CancellationToken cancellationToken)
    {
        Guid assistantMessageId = Guid.Empty;

        try
        {
            await foreach (AgentEvent agentEvent in sessionManager.SendMessageAsync(
                               childChatId,
                               content,
                               cancellationToken))
            {
                if (assistantMessageId == Guid.Empty)
                {
                    ChatSession? session = sessionManager.GetSession(childChatId);
                    ChatMessage? assistant = session?.Messages.LastOrDefault(message =>
                        string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase));

                    if (assistant is not null)
                    {
                        assistantMessageId = assistant.Id;
                    }
                }

                OrchestrationEvent? mapped = MapAgentEvent(childChatId, assistantMessageId, agentEvent);
                if (mapped is not null)
                {
                    await eventHub.PublishAsync(parentChatId, mapped, cancellationToken);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Orchestration agent run failed for child chat {ChildChatId}", childChatId);

            await eventHub.PublishAsync(
                parentChatId,
                new OrchestrationAgentErrorEvent(childChatId, "Orchestration.RunFailed", ex.Message),
                cancellationToken);
        }
    }

    private static OrchestrationEvent? MapAgentEvent(
        Guid childChatId,
        Guid assistantMessageId,
        AgentEvent agentEvent)
    {
        return agentEvent switch
        {
            AgentStatusEvent status => new OrchestrationAgentStatusEvent(childChatId, status.Phase),
            AgentTextDeltaEvent delta => new OrchestrationAgentTokenEvent(childChatId, delta.Text),
            AgentToolEvent tool => new OrchestrationAgentToolEvent(childChatId, tool.Label),
            AgentScriptEvent script => new OrchestrationAgentToolEvent(childChatId, script.StepLabel),
            AgentCompletedEvent => new OrchestrationAgentDoneEvent(
                childChatId,
                assistantMessageId,
                Succeeded: true),
            AgentErrorEvent error => new OrchestrationAgentErrorEvent(
                childChatId,
                error.Code,
                error.Message),
            _ => null
        };
    }
}
