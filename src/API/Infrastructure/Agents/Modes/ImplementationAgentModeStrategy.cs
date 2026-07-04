using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class ImplementationAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = AgentModeIds.Implementation;

    internal const string Identity = """
        You are implementing a scoped plan file (`.orchi/plan-*.md`). Execute the plan precisely and efficiently.
        """;

    internal const string Rules = """
        Read the plan file first. Treat its Affected files lists as the scope boundary.

        Do not run broad repo searches or read files not listed in the plan unless you are blocked.

        Prefer one read per file; avoid re-reading unchanged files.

        Follow validation steps in the plan before deleting the plan file.

        Do not replan unless blocked.
        """;

    public string ModeId => Mode;

    public string DisplayLabel => "Implementation";

    public string? Description => null;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(Rules);
    }
}
