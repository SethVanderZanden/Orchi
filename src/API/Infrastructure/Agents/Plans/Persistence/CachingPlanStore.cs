using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Plans.Persistence;

public sealed class CachingPlanStore(
    EfPlanStore inner,
    IOrchiCacheService cache,
    OrchiHybridCacheService cacheOptions) : IPlanStore
{
    public async Task UpsertAsync(PlanUpsertModel model, CancellationToken cancellationToken)
    {
        await inner.UpsertAsync(model, cancellationToken);
        await cache.RemoveAsync(
            OrchiCacheKeys.Plan(model.SourceChatId, model.PlanId),
            cancellationToken);
    }

    public async Task<StoredPlan?> GetAsync(
        Guid sourceChatId,
        string planId,
        CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            OrchiCacheKeys.Plan(sourceChatId, planId),
            async ct => await inner.GetAsync(sourceChatId, planId, ct),
            cacheOptions.CreatePlanEntryOptions(),
            cancellationToken);

    public Task<IReadOnlyList<StoredPlan>> ListBySourceChatAsync(
        Guid sourceChatId,
        CancellationToken cancellationToken) =>
        // List results stay uncached: upserts invalidate per-plan keys, not a list key.
        inner.ListBySourceChatAsync(sourceChatId, cancellationToken);
}
