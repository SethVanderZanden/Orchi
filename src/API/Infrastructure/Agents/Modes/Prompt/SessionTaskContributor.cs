namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class SessionTaskContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (string.IsNullOrWhiteSpace(context.PlanFilePath))
        {
            return;
        }

        document.Task =
            $"Implement the plan at `{context.PlanFilePath.Trim()}`. Follow the plan precisely. Do not replan unless blocked.";
    }
}
