using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt.Behaviours;

internal sealed class LoggingPromptComposer(
    IAgentPromptComposer innerComposer,
    ILogger<LoggingPromptComposer> logger) : IAgentPromptComposer
{
    public string Compose(ChatSession session, string userContent)
    {
        logger.LogDebug(
            "Composing prompt for chat {ChatId} in mode {Mode}",
            session.Id,
            session.Mode);

        string prompt = innerComposer.Compose(session, userContent);

        logger.LogDebug(
            "Composed prompt for chat {ChatId}: {PromptLength} characters",
            session.Id,
            prompt.Length);

        return prompt;
    }

    public IReadOnlyList<string> GetExtraCliArgs(string modeId) =>
        innerComposer.GetExtraCliArgs(modeId);
}
