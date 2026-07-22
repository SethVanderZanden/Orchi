using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

/// <summary>
/// Default review diff: live workspace <c>git diff HEAD</c> / <c>git show HEAD</c>.
/// </summary>
public sealed class WorkspaceHeadReviewDiffAdapter(IWorkspaceDiffProvider diffProvider) : IReviewDiffAdapter
{
    public int Order => 100;

    public bool CanHandle(PromptBuildContext context) =>
        context.PlanFilePath is not null &&
        context.PlanFilePath.Replace('\\', '/').Contains("/review-", StringComparison.OrdinalIgnoreCase);

    public ReviewDiffPayload GetDiff(PromptBuildContext context) =>
        new(
            "Implementation changes (captured from workspace; future versions may use snapshots instead of live git diff):",
            diffProvider.GetDiff(context.WorkspacePath));
}
