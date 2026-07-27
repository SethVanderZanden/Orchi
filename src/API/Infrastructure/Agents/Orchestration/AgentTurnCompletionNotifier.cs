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
                AgentSessionManager sessionManager =
                    scope.ServiceProvider.GetRequiredService<AgentSessionManager>();
                IOrchestrationWorkflowService workflow =
                    scope.ServiceProvider.GetRequiredService<IOrchestrationWorkflowService>();

                if (succeeded)
                {
                    ChatSession? completedChat =
                        await sessionManager.GetOrLoadSessionAsync(chatId, CancellationToken.None);

                    if (completedChat is not null)
                    {
                        IOrchestrationPlanSyncService planSync =
                            scope.ServiceProvider.GetRequiredService<IOrchestrationPlanSyncService>();
                        await planSync.SyncFromWorkspaceAsync(completedChat, CancellationToken.None);
                    }
                }

                await workflow.OnAgentTurnCompletedAsync(chatId, succeeded, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Orchestration turn completion failed for chat {ChatId}", chatId);
            }
        });
    }
}
