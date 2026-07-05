namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public interface IAgentTurnCompletionNotifier
{
    void NotifyTurnCompleted(Guid chatId, bool succeeded);
}
