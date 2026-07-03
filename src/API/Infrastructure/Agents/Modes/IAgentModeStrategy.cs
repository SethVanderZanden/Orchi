namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IAgentModeStrategy
{
    string ModeId { get; }

    string BuildPrompt(string userContent);

    IReadOnlyList<string> ExtraCliArgs { get; }
}
