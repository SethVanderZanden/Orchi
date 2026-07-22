using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Codex;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed class AgentModelCatalogService(
    IAgentModelStore store,
    AgentModelListProviderFactory providerFactory,
    IAgentAdapterFactory adapterFactory,
    OrchiHybridCacheService cache) : IAgentModelCatalogService
{
    public async Task<IReadOnlyList<AgentModelDto>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        return await cache.GetOrCreateAsync(
            OrchiCacheKeys.AgentModels(agentId, includeDisabled),
            async ct =>
            {
                IReadOnlyList<StoredAgentModel> models = await store.ListAsync(agentId, includeDisabled, ct);
                return models.Select(ToDto).ToArray();
            },
            cache.CreateAgentModelsEntryOptions(),
            cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLastSyncedAtAsync(string agentId, CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);
        return await store.GetLastSyncedAtAsync(agentId, cancellationToken);
    }

    public async Task<Result<AgentModelSyncResult>> SyncAsync(string agentId, CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        IAgentModelListProvider provider = providerFactory.GetProvider(agentId);
        IReadOnlyList<AgentModelListEntry> fetched;

        try
        {
            fetched = await provider.FetchModelsAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or not OperationCanceledException)
        {
            return Result.Failure<AgentModelSyncResult>(
                Error.Validation("Model.SyncFailed", ex.Message));
        }

        DateTimeOffset syncedAt = DateTimeOffset.UtcNow;
        IReadOnlyList<CliModelMergeEntry> mergeEntries = fetched
            .Select(entry => new CliModelMergeEntry(entry.ModelId, entry.Label, entry.IsDefault, entry.IsCurrent))
            .ToArray();

        await store.MergeCliModelsAsync(agentId, mergeEntries, syncedAt, cancellationToken);
        await InvalidateCacheAsync(agentId, cancellationToken);

        IReadOnlyList<AgentModelDto> models = await ListAsync(agentId, includeDisabled: true, cancellationToken);
        return Result.Success(new AgentModelSyncResult(models, syncedAt));
    }

    public async Task<Result<AgentModelDto>> AddManualAsync(
        string agentId,
        string modelId,
        string? label,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            return Result.Failure<AgentModelDto>(
                Error.Validation("Model.Required", "Model id is required."));
        }

        string trimmed = modelId.Trim();
        StoredAgentModel stored = await store.AddManualAsync(agentId, trimmed, label, cancellationToken);
        await InvalidateCacheAsync(agentId, cancellationToken);
        return Result.Success(ToDto(stored));
    }

    public async Task EnsureBuiltInModelsAsync(string agentId, CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        if (!string.Equals(agentId, AgentIds.Codex, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (CodexBuiltInCatalog.ModelPreset preset in CodexBuiltInCatalog.ModelPresets)
        {
            await store.EnsureBuiltInAsync(
                agentId,
                preset.ModelId,
                preset.Label,
                preset.IsDefault,
                cancellationToken);
        }

        await InvalidateCacheAsync(agentId, cancellationToken);
    }

    public async Task<Result<AgentModelDto>> UpdateEnabledAsync(
        string agentId,
        string modelId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        StoredAgentModel? updated = await store.UpdateEnabledAsync(agentId, modelId, enabled, cancellationToken);
        if (updated is null)
        {
            return Result.Failure<AgentModelDto>(Error.NotFound($"Model '{modelId}' was not found."));
        }

        await InvalidateCacheAsync(agentId, cancellationToken);
        return Result.Success(ToDto(updated));
    }

    public async Task<Result> RemoveAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            return Result.Failure(Error.Validation("Model.Required", "Model id is required."));
        }

        bool removed = await store.RemoveAsync(agentId, modelId.Trim(), cancellationToken);
        if (!removed)
        {
            return Result.Failure(Error.NotFound($"Model '{modelId}' was not found."));
        }

        await InvalidateCacheAsync(agentId, cancellationToken);
        return Result.Success();
    }

    public async Task<bool> IsEnabledModelAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken)
    {
        StoredAgentModel? model = await store.GetAsync(agentId, modelId, cancellationToken);
        return model is not null && model.IsEnabled;
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
        await cache.RemoveAsync(OrchiCacheKeys.AgentModels(agentId, includeDisabled: false), cancellationToken);
        await cache.RemoveAsync(OrchiCacheKeys.AgentModels(agentId, includeDisabled: true), cancellationToken);
    }

    private static AgentModelDto ToDto(StoredAgentModel model) =>
        new(
            model.ModelId,
            model.Label,
            model.IsDefault,
            model.IsCurrent,
            model.IsEnabled,
            model.Source);
}
