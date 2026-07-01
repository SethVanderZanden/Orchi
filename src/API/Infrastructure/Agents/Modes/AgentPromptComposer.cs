using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.SharedContext.Modes;
using Orchi.SharedContext.Prompts;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class AgentPromptComposer(IPromptBuilder promptBuilder, IModeRuntime modeRuntime)
{
    public async Task<Result<AgentTurnRequest>> ComposeAsync(
        ChatSession session,
        string userContent,
        string modeInstructions,
        string? middleSection,
        CancellationToken cancellationToken)
    {
        bool forceAsk = userContent.StartsWith("[goal-check-in]", StringComparison.Ordinal);
        string modeKey = ChatModeParser.ToApiString(session.Mode);

        var context = new PromptSessionContext(
            session.WorkspacePath,
            modeKey,
            session.Id,
            session.ExternalSessionId,
            session.PreviousModeKey,
            session.ModeChangedAt,
            session.Messages
                .Select(message => new PriorChatMessage(message.Role, message.Content, message.Status))
                .ToList(),
            userContent,
            middleSection,
            forceAsk);

        PromptBuildResult built = await promptBuilder.BuildTurnAsync(context, modeInstructions, cancellationToken);

        CursorCliProfile profile = forceAsk
            ? modeRuntime.ResolveCliProfile("participant")
            : modeRuntime.ResolveCliProfile(modeKey);

        return Result.Success(new AgentTurnRequest(
            built.StablePrefix,
            built.DynamicContext,
            profile.ExtraArgs.ToList(),
            profile.Kind));
    }
}
