using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Workspace;

namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class ReviewDiffContributor(IReviewDiffAdapterResolver diffAdapterResolver) : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (!AgentModeIds.IsReviewFamily(context.ModeId))
        {
            return;
        }

        ReviewDiffPayload? payload = diffAdapterResolver.Resolve(context);
        if (payload is null)
        {
            return;
        }

        document.AppendContext(
            $"""
            {payload.Intro}

            {payload.Diff}
            """);
    }
}
