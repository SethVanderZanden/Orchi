using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Codex;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed record AgentCliOptionDto(
    string Kind,
    string Id,
    string Label,
    string CliValue,
    bool IsEnabled,
    string Source);

public interface IAgentCliOptionCatalogService
{
    Task<IReadOnlyList<AgentCliOptionDto>> ListAsync(
        string agentId,
        string kind,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<Result<AgentCliOptionDto>> AddManualAsync(
        string agentId,
        string kind,
        string optionId,
        string label,
        string? cliValue,
        CancellationToken cancellationToken);

    Task<Result<AgentCliOptionDto>> UpdateEnabledAsync(
        string agentId,
        string kind,
        string optionId,
        bool isEnabled,
        CancellationToken cancellationToken);

    Task<Result> RemoveAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken);

    Task<bool> IsEnabledOptionAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken);

    Task<string?> ResolveCliValueAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken);

    Task EnsureBuiltInPresetsAsync(string agentId, CancellationToken cancellationToken);
}

public sealed class AgentCliOptionCatalogService(
    IAgentCliOptionStore store,
    IAgentAdapterFactory adapterFactory,
    OrchiHybridCacheService cache) : IAgentCliOptionCatalogService
{
    public async Task<IReadOnlyList<AgentCliOptionDto>> ListAsync(
        string agentId,
        string kind,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        string normalizedKind = RequireKnownKind(kind);
        ValidateAgent(agentId);

        return await cache.GetOrCreateAsync(
            OrchiCacheKeys.AgentCliOptions(agentId, normalizedKind, includeDisabled),
            async ct =>
            {
                IReadOnlyList<StoredAgentCliOption> options =
                    await store.ListAsync(agentId, normalizedKind, includeDisabled, ct);
                return options.Select(ToDto).ToArray();
            },
            cache.CreateAgentModelsEntryOptions(),
            cancellationToken);
    }

    public async Task<Result<AgentCliOptionDto>> AddManualAsync(
        string agentId,
        string kind,
        string optionId,
        string label,
        string? cliValue,
        CancellationToken cancellationToken)
    {
        string normalizedKind = RequireKnownKind(kind);
        ValidateAgent(agentId);

        if (string.IsNullOrWhiteSpace(optionId))
        {
            return Result.Failure<AgentCliOptionDto>(
                Error.Validation("CliOption.Required", "Option id is required."));
        }

        string trimmedId = optionId.Trim();
        string trimmedLabel = string.IsNullOrWhiteSpace(label) ? trimmedId : label.Trim();
        string trimmedCliValue = string.IsNullOrWhiteSpace(cliValue) ? trimmedId : cliValue.Trim();

        StoredAgentCliOption? existing = await store.GetAsync(
            agentId,
            normalizedKind,
            trimmedId,
            cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<AgentCliOptionDto>(
                Error.Validation(
                    "CliOption.Duplicate",
                    $"Option '{trimmedId}' already exists for '{normalizedKind}'."));
        }

        StoredAgentCliOption stored = await store.AddManualAsync(
            agentId,
            normalizedKind,
            trimmedId,
            trimmedLabel,
            trimmedCliValue,
            cancellationToken);

        await InvalidateCacheAsync(agentId, normalizedKind, cancellationToken);
        return Result.Success(ToDto(stored));
    }

    public async Task<Result<AgentCliOptionDto>> UpdateEnabledAsync(
        string agentId,
        string kind,
        string optionId,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        string normalizedKind = RequireKnownKind(kind);
        ValidateAgent(agentId);

        StoredAgentCliOption? updated = await store.UpdateEnabledAsync(
            agentId,
            normalizedKind,
            optionId.Trim(),
            isEnabled,
            cancellationToken);

        if (updated is null)
        {
            return Result.Failure<AgentCliOptionDto>(
                Error.NotFound($"Option '{optionId}' was not found for '{normalizedKind}'."));
        }

        await InvalidateCacheAsync(agentId, normalizedKind, cancellationToken);
        return Result.Success(ToDto(updated));
    }

    public async Task<Result> RemoveAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken)
    {
        string normalizedKind = RequireKnownKind(kind);
        ValidateAgent(agentId);

        bool removed = await store.RemoveAsync(
            agentId,
            normalizedKind,
            optionId.Trim(),
            cancellationToken);

        if (!removed)
        {
            return Result.Failure(
                Error.NotFound($"Option '{optionId}' was not found for '{normalizedKind}'."));
        }

        await InvalidateCacheAsync(agentId, normalizedKind, cancellationToken);
        return Result.Success();
    }

    public async Task<bool> IsEnabledOptionAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken)
    {
        string normalizedKind = RequireKnownKind(kind);
        StoredAgentCliOption? option = await store.GetAsync(
            agentId,
            normalizedKind,
            optionId,
            cancellationToken);

        return option is { IsEnabled: true };
    }

    public async Task<string?> ResolveCliValueAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken)
    {
        string normalizedKind = RequireKnownKind(kind);
        StoredAgentCliOption? option = await store.GetAsync(
            agentId,
            normalizedKind,
            optionId,
            cancellationToken);

        if (option is null || !option.IsEnabled)
        {
            return null;
        }

        return option.CliValue;
    }

    public async Task EnsureBuiltInPresetsAsync(string agentId, CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        if (!string.Equals(agentId, AgentIds.Codex, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (CodexBuiltInCatalog.CliOptionPreset preset in CodexBuiltInCatalog.AllCliOptionPresets)
        {
            await store.EnsureBuiltInAsync(
                agentId,
                preset.Kind,
                preset.OptionId,
                preset.Label,
                preset.CliValue,
                cancellationToken);
        }

        await InvalidateKindCacheAsync(agentId, AgentCliOptionKinds.ModelReasoningEffort, cancellationToken);
        await InvalidateKindCacheAsync(agentId, AgentCliOptionKinds.ApprovalPolicy, cancellationToken);
    }

    private static string RequireKnownKind(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("CLI option kind is required.", nameof(kind));
        }

        string normalized = AgentCliOptionKinds.Normalize(kind);
        if (!AgentCliOptionKinds.IsKnown(normalized))
        {
            throw new ArgumentException(
                $"Unsupported CLI option kind '{kind}'.",
                nameof(kind));
        }

        return normalized;
    }

    private void ValidateAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent id is required.", nameof(agentId));
        }

        _ = adapterFactory.GetAdapter(agentId);
    }

    private async Task InvalidateKindCacheAsync(
        string agentId,
        string kind,
        CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(OrchiCacheKeys.AgentCliOptions(agentId, kind, true), cancellationToken);
        await cache.RemoveAsync(OrchiCacheKeys.AgentCliOptions(agentId, kind, false), cancellationToken);
    }

    private async Task InvalidateCacheAsync(
        string agentId,
        string kind,
        CancellationToken cancellationToken)
    {
        await InvalidateKindCacheAsync(agentId, kind, cancellationToken);
    }

    private static AgentCliOptionDto ToDto(StoredAgentCliOption option) =>
        new(option.Kind, option.OptionId, option.Label, option.CliValue, option.IsEnabled, option.Source);
}
