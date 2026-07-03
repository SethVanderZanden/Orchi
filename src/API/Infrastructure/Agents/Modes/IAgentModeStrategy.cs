using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IAgentModeStrategy
{
    string ModeId { get; }

    void ContributeSections(PromptBuildContext context, OrchiPromptDocument document);

    IReadOnlyList<string> ExtraCliArgs { get; }
}
