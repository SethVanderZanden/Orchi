using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class SessionTaskContributor(IOrchiArtifactTaskFactory artifactTaskFactory) : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (!context.IsFirstUserTurn)
        {
            return;
        }

        string? task = artifactTaskFactory.ResolveTaskFromPath(context.PlanFilePath);
        if (task is null)
        {
            return;
        }

        document.Task = task;
    }
}
