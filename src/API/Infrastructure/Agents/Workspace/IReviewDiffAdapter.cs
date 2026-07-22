using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

/// <summary>
/// Selects how to capture the change set for a review prompt (workspace HEAD vs branch pair, etc.).
/// </summary>
public interface IReviewDiffAdapter
{
    /// <summary>Lower runs first. Prefer specific adapters over the workspace-head fallback.</summary>
    int Order { get; }

    bool CanHandle(PromptBuildContext context);

    ReviewDiffPayload GetDiff(PromptBuildContext context);
}

public sealed record ReviewDiffPayload(string Intro, string Diff);
