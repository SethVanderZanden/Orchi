namespace Orchi.Api.Infrastructure.Agents.Modes;

public static class AgentModeIds
{
    public const string Default = "default";

    public const string Orchestration = "orchestration";

    /// <summary>Review of completed implementation work against its plan.</summary>
    public const string Review = "review";

    /// <summary>Pull-request style review of a head branch against a base branch.</summary>
    public const string BranchReview = "branch-review";

    public const string Implementation = "implementation";

    public static bool IsReviewFamily(string? modeId) =>
        string.Equals(modeId, Review, StringComparison.OrdinalIgnoreCase)
        || string.Equals(modeId, BranchReview, StringComparison.OrdinalIgnoreCase);

    public static bool IsKickoffOnly(string? modeId) =>
        string.Equals(modeId, Implementation, StringComparison.OrdinalIgnoreCase)
        || string.Equals(modeId, BranchReview, StringComparison.OrdinalIgnoreCase);
}
