using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed record AgentContextSizeDto(
    string Id,
    string Label,
    int TokenCount,
    bool IsEnabled,
    string Source);

public interface IAgentContextSizeCatalogService
{
    Task<IReadOnlyList<AgentContextSizeDto>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<Result<AgentContextSizeDto>> AddManualAsync(
        string agentId,
        string sizeId,
        string label,
        int tokenCount,
        CancellationToken cancellationToken);

    Task<Result<AgentContextSizeDto>> UpdateEnabledAsync(
        string agentId,
        string sizeId,
        bool isEnabled,
        CancellationToken cancellationToken);

    Task<Result> RemoveAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken);

    Task<bool> IsEnabledSizeAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken);

    Task<int?> ResolveTokenCountAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken);
}

public sealed class AgentContextSizeCatalogService(
    IAgentContextSizeStore store,
    IAgentAdapterFactory adapterFactory,
    OrchiHybridCacheService cache) : IAgentContextSizeCatalogService
{
    public async Task<IReadOnlyList<AgentContextSizeDto>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        return await cache.GetOrCreateAsync(
            OrchiCacheKeys.AgentContextSizes(agentId, includeDisabled),
            async ct =>
            {
                IReadOnlyList<StoredAgentContextSize> sizes = await store.ListAsync(agentId, includeDisabled, ct);
                return sizes.Select(ToDto).ToArray();
            },
            cache.CreateAgentModelsEntryOptions(),
            cancellationToken);
    }

    public async Task<Result<AgentContextSizeDto>> AddManualAsync(
        string agentId,
        string sizeId,
        string label,
        int tokenCount,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        if (string.IsNullOrWhiteSpace(sizeId))
        {
            return Result.Failure<AgentContextSizeDto>(
                Error.Validation("ContextSize.Required", "Context size id is required."));
        }

        if (tokenCount <= 0)
        {
            return Result.Failure<AgentContextSizeDto>(
                Error.Validation("ContextSize.TokenCount", "Token count must be greater than zero."));
        }

        string trimmedId = sizeId.Trim();
        string trimmedLabel = string.IsNullOrWhiteSpace(label) ? trimmedId : label.Trim();

        StoredAgentContextSize? existing = await store.GetAsync(agentId, trimmedId, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<AgentContextSizeDto>(
                Error.Validation("ContextSize.Duplicate", $"Context size '{trimmedId}' already exists."));
        }

        StoredAgentContextSize stored = await store.AddManualAsync(
            agentId,
            trimmedId,
            trimmedLabel,
            tokenCount,
            cancellationToken);

        await InvalidateCacheAsync(agentId, cancellationToken);
        return Result.Success(ToDto(stored));
    }

    public async Task<Result<AgentContextSizeDto>> UpdateEnabledAsync(
        string agentId,
        string sizeId,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        StoredAgentContextSize? updated = await store.UpdateEnabledAsync(
            agentId,
            sizeId.Trim(),
            isEnabled,
            cancellationToken);

        if (updated is null)
        {
            return Result.Failure<AgentContextSizeDto>(
                Error.NotFound($"Context size '{sizeId}' was not found."));
        }

        await InvalidateCacheAsync(agentId, cancellationToken);
        return Result.Success(ToDto(updated));
    }

    public async Task<Result> RemoveAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        bool removed = await store.RemoveAsync(agentId, sizeId.Trim(), cancellationToken);
        if (!removed)
        {
            return Result.Failure(Error.NotFound($"Context size '{sizeId}' was not found."));
        }

        await InvalidateCacheAsync(agentId, cancellationToken);
        return Result.Success();
    }

    public async Task<bool> IsEnabledSizeAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken)
    {
        StoredAgentContextSize? size = await store.GetAsync(agentId, sizeId, cancellationToken);
        return size is { IsEnabled: true };
    }

    public async Task<int?> ResolveTokenCountAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken)
    {
        StoredAgentContextSize? size = await store.GetAsync(agentId, sizeId, cancellationToken);
        if (size is null || !size.IsEnabled)
        {
            return null;
        }

        return size.TokenCount;
    }

    private void ValidateAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent id is required.", nameof(agentId));
        }

        _ = adapterFactory.GetAdapter(agentId);
    }

    private async Task InvalidateCacheAsync(string agentId, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(OrchiCacheKeys.AgentContextSizes(agentId, true), cancellationToken);
        await cache.RemoveAsync(OrchiCacheKeys.AgentContextSizes(agentId, false), cancellationToken);
    }

    private static AgentContextSizeDto ToDto(StoredAgentContextSize size) =>
        new(size.SizeId, size.Label, size.TokenCount, size.IsEnabled, size.Source);
}
