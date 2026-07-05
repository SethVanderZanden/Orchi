using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

namespace Orchi.Api.Infrastructure.Agents.Orchestration.Handlers;

public sealed class ReviewKickoffStepHandler(IOrchiArtifactWriterFactory artifactWriterFactory)
    : IOrchestrationStepHandler
{
    public Task<OrchestrationStepResult?> HandleAsync(
        OrchestrationStepContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Succeeded)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        if (!string.Equals(context.CompletedChat.Mode, ImplementationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        if (context.CompletedPlanId is null)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        string expectedReviewPath = artifactWriterFactory
            .GetStrategy(OrchiArtifactKind.Review)
            .BuildRelativePath(context.CompletedPlanId);

        bool reviewExists = context.ChildChats.Any(chat =>
            chat.ParentChatId == context.ParentChatId &&
            string.Equals(chat.PlanFilePath, expectedReviewPath, StringComparison.OrdinalIgnoreCase));

        if (reviewExists)
        {
            return Task.FromResult<OrchestrationStepResult?>(null);
        }

        return Task.FromResult<OrchestrationStepResult?>(
            new OrchestrationStepResult([
                new OrchestrationStepAction(
                    OrchestrationStepActionKind.KickOffReview,
                    ImplementationChildChatId: context.CompletedChat.Id)
            ]));
    }
}
