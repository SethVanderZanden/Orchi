namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class DefaultAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = "default";

    public string ModeId => Mode;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public string BuildPrompt(string userContent) => userContent;
}
