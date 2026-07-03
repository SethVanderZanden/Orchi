using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Workspace;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class ReviewDiffContributor(IWorkspaceDiffProvider diffProvider) : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (!string.Equals(context.ModeId, ReviewAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.PlanFilePath is null ||
            !context.PlanFilePath.Replace('\\', '/').Contains("/review-", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string diff = diffProvider.GetDiff(context.WorkspacePath);
        document.AppendContext(
            $"""
            Implementation changes (captured from workspace; future versions may use snapshots instead of live git diff):

            {diff}
            """);
    }
}
