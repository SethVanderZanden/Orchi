namespace Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;

public interface IOrchestrationWorkflowStore
{
    Task<OrchestrationWorkflowRecord?> GetAsync(Guid parentChatId, CancellationToken cancellationToken);

    Task UpsertAsync(OrchestrationWorkflowRecord record, CancellationToken cancellationToken);
}
