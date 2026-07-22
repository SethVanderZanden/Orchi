using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

public interface IReviewDiffAdapterResolver
{
    ReviewDiffPayload? Resolve(PromptBuildContext context);
}

public sealed class ReviewDiffAdapterResolver(IEnumerable<IReviewDiffAdapter> adapters) : IReviewDiffAdapterResolver
{
    private readonly IReadOnlyList<IReviewDiffAdapter> _adapters =
        adapters.OrderBy(adapter => adapter.Order).ToArray();

    public ReviewDiffPayload? Resolve(PromptBuildContext context)
    {
        foreach (IReviewDiffAdapter adapter in _adapters)
        {
            if (adapter.CanHandle(context))
            {
                return adapter.GetDiff(context);
            }
        }

        return null;
    }
}
