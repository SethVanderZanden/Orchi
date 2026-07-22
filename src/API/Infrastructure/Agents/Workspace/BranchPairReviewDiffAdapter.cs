using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

/// <summary>
/// PR-style review: three-dot diff from the <c>orchi-branch-review</c> brief marker.
/// </summary>
public sealed class BranchPairReviewDiffAdapter(IWorkspaceDiffProvider diffProvider) : IReviewDiffAdapter
{
    public int Order => 0;

    public bool CanHandle(PromptBuildContext context)
    {
        if (!IsReviewPlanPath(context.PlanFilePath))
        {
            return false;
        }

        return BranchReviewBriefParser.TryParseFromFile(context.WorkspacePath, context.PlanFilePath) is not null;
    }

    public ReviewDiffPayload GetDiff(PromptBuildContext context)
    {
        BranchReviewBriefParser.BranchReviewRefs refs =
            BranchReviewBriefParser.TryParseFromFile(context.WorkspacePath, context.PlanFilePath)
            ?? throw new InvalidOperationException("Branch review refs were expected but not found.");

        string diff = diffProvider.GetBranchDiff(
            context.WorkspacePath,
            refs.BaseBranch,
            refs.HeadBranch);

        return new ReviewDiffPayload(
            $"Pull request changes (`{refs.BaseBranch}...{refs.HeadBranch}`):",
            diff);
    }

    private static bool IsReviewPlanPath(string? planFilePath) =>
        planFilePath is not null &&
        planFilePath.Replace('\\', '/').Contains("/review-", StringComparison.OrdinalIgnoreCase);
}
