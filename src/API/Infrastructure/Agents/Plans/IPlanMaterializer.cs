using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public interface IPlanMaterializer
{
    /// <summary>
    /// Persist orchestration plans to the plan store and <c>.orchi/plan-*.md</c> files.
    /// Prefers plan blocks from the latest assistant message; otherwise syncs from existing files.
    /// </summary>
    Task<IReadOnlyList<StoredPlan>> MaterializeAsync(
        ChatSession orchestrationChat,
        CancellationToken cancellationToken = default);
}
