namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public sealed class OrchestrationStepPipeline(IEnumerable<IOrchestrationStepHandler> handlers)
{
    private readonly IReadOnlyList<IOrchestrationStepHandler> _handlers = handlers.ToList();

    public async Task<IReadOnlyList<OrchestrationStepAction>> ExecuteAsync(
        OrchestrationStepContext context,
        CancellationToken cancellationToken)
    {
        var actions = new List<OrchestrationStepAction>();

        foreach (IOrchestrationStepHandler handler in _handlers)
        {
            OrchestrationStepResult? result = await handler.HandleAsync(context, cancellationToken);
            if (result is null)
            {
                continue;
            }

            actions.AddRange(result.Actions);
        }

        return actions;
    }
}
