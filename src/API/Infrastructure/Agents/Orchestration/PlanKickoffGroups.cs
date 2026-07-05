using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public static class PlanKickoffGroups
{
    public static (IReadOnlyList<PlanMarkdownParser.ParsedPlan> Sequenced, IReadOnlyList<PlanMarkdownParser.ParsedPlan> Independent)
        Resolve(
            IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans,
            IReadOnlyList<string> sequencePlanIds)
    {
        var planById = plans.ToDictionary(plan => plan.PlanId, StringComparer.OrdinalIgnoreCase);
        var sequenceSet = new HashSet<string>(sequencePlanIds, StringComparer.OrdinalIgnoreCase);

        var sequenced = new List<PlanMarkdownParser.ParsedPlan>();
        foreach (string planId in sequencePlanIds)
        {
            if (planById.TryGetValue(planId, out PlanMarkdownParser.ParsedPlan? plan))
            {
                sequenced.Add(plan);
            }
        }

        IReadOnlyList<PlanMarkdownParser.ParsedPlan> independent = plans
            .Where(plan => !sequenceSet.Contains(plan.PlanId))
            .ToArray();

        return (sequenced, independent);
    }
}
