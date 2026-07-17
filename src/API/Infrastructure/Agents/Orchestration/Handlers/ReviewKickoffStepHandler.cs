using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.UserPreferences;

namespace Orchi.Api.Infrastructure.Agents.Orchestration.Handlers;

public sealed class ReviewKickoffStepHandler(
    IOrchiArtifactWriterFactory artifactWriterFactory,
    IUserPreferenceService preferenceService)
    : IOrchestrationStepHandler
{
    public async Task<OrchestrationStepResult?> HandleAsync(
        OrchestrationStepContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Succeeded)
        {
            return null;
        }

        if (!string.Equals(
                context.CompletedChat.Mode,
                ImplementationAgentModeStrategy.Mode,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (context.CompletedPlanId is null)
        {
            return null;
        }

        UserPreferenceDto preferences = await preferenceService.GetAsync(cancellationToken);
        if (!preferences.AutoKickOffReview)
        {
            return null;
        }

        string expectedReviewPath = artifactWriterFactory
            .GetStrategy(OrchiArtifactKind.Review)
            .BuildRelativePath(context.CompletedPlanId);

        bool reviewExists = context.ChildChats.Any(chat =>
            chat.ParentChatId == context.ParentChatId &&
            string.Equals(chat.PlanFilePath, expectedReviewPath, StringComparison.OrdinalIgnoreCase));

        if (reviewExists)
        {
            return null;
        }

        return new OrchestrationStepResult([
            new OrchestrationStepAction(
                OrchestrationStepActionKind.KickOffReview,
                ImplementationChildChatId: context.CompletedChat.Id)
        ]);
    }
}
