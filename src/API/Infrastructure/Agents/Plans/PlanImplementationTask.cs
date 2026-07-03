namespace Orchi.Api.Infrastructure.Agents.Plans;

public static class PlanImplementationTask
{
    public static string Build(string planFilePath)
    {
        string path = planFilePath.Trim();
        return
            $"Implement the plan at `{path}`. Follow the plan precisely. Do not replan unless blocked. " +
            $"After the plan is fully implemented and validated, delete `{path}`. " +
            "If blocked, keep the plan file.";
    }
}
