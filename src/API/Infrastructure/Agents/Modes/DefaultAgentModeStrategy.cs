using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class DefaultAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = AgentModeIds.Default;

    public string ModeId => Mode;

    public string DisplayLabel => "Default";

    public string? Description => null;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
    }
}
