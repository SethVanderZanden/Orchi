using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class DefaultAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = "default";

    public string ModeId => Mode;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
    }
}
