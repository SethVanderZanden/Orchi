using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
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

        BranchReviewBriefParser.BranchReviewRefs? branchRefs =
            BranchReviewBriefParser.TryParseFromFile(context.WorkspacePath, context.PlanFilePath);

        string diff = branchRefs is null
            ? diffProvider.GetDiff(context.WorkspacePath)
            : diffProvider.GetBranchDiff(
                context.WorkspacePath,
                branchRefs.BaseBranch,
                branchRefs.HeadBranch);

        string intro = branchRefs is null
            ? "Implementation changes (captured from workspace; future versions may use snapshots instead of live git diff):"
            : $"Pull request changes (`{branchRefs.BaseBranch}...{branchRefs.HeadBranch}`):";

        document.AppendContext(
            $"""
            {intro}

            {diff}
            """);
    }
}
