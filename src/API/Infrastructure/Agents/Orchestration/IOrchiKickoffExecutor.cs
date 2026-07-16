using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public interface IOrchiKickoffExecutor
{
    Task KickOffPlanAndRunAsync(
        Guid parentChatId,
        PlanMarkdownParser.ParsedPlan plan,
        CancellationToken cancellationToken);

    Task KickOffReviewAndRunAsync(
        Guid parentChatId,
        Guid implementationChildChatId,
        CancellationToken cancellationToken);

    void RunAgentTurnInBackground(Guid parentChatId, Guid childChatId, string content);
}
