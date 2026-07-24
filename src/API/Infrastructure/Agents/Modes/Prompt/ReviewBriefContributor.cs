using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

/// <summary>
/// Injects the on-disk review brief into the prompt so review agents have the original plan
/// and branch instructions without a separate file-read tool turn (which often stalls kickoff).
/// </summary>
public sealed class ReviewBriefContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (!string.Equals(context.ModeId, ReviewAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(context.PlanFilePath)
            || string.IsNullOrWhiteSpace(context.WorkspacePath))
        {
            return;
        }

        if (!IsReviewBriefPath(context.PlanFilePath))
        {
            return;
        }

        string fullPath = Path.Combine(
            context.WorkspacePath,
            context.PlanFilePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            document.AppendContext(
                $"Review brief file is missing at `{context.PlanFilePath}`. Say exactly what is missing.");
            return;
        }

        string brief = File.ReadAllText(fullPath).Trim();
        if (string.IsNullOrWhiteSpace(brief))
        {
            document.AppendContext(
                $"Review brief at `{context.PlanFilePath}` is empty. Say exactly what is missing.");
            return;
        }

        document.AppendContext($"Review brief:\n\n{brief}");
    }

    private static bool IsReviewBriefPath(string planFilePath) =>
        planFilePath.Replace('\\', '/').Contains("/review-", StringComparison.OrdinalIgnoreCase);
}
