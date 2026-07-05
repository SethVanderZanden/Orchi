namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public sealed class AgentTurnCompletionNotifier(
    IServiceScopeFactory scopeFactory,
    ILogger<AgentTurnCompletionNotifier> logger) : IAgentTurnCompletionNotifier
{
    public void NotifyTurnCompleted(Guid chatId, bool succeeded)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IOrchestrationWorkflowService workflow =
                    scope.ServiceProvider.GetRequiredService<IOrchestrationWorkflowService>();

                await workflow.OnAgentTurnCompletedAsync(chatId, succeeded, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Orchestration turn completion failed for chat {ChatId}", chatId);
            }
        });
    }
}
