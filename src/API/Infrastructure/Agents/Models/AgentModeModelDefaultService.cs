using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed class AgentModeModelDefaultService(
    IAgentModeModelDefaultStore store,
    IAgentModelCatalogService catalogService,
    IAgentAdapterFactory adapterFactory,
    IEnumerable<IAgentModeStrategy> modeStrategies) : IAgentModeModelDefaultService
{
    private static readonly string[] ModeOrder =
    [
        AgentModeIds.Default,
        AgentModeIds.Orchestration,
        AgentModeIds.Review,
        AgentModeIds.Implementation
    ];

    public async Task<IReadOnlyList<AgentModeModelDefaultDto>> ListAsync(
        string agentId,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        IReadOnlyList<StoredAgentModeModelDefault> stored = await store.ListAsync(agentId, cancellationToken);
        var storedByMode = stored.ToDictionary(row => row.Mode, StringComparer.OrdinalIgnoreCase);

        return ModeOrder
            .Select(modeId =>
            {
                IAgentModeStrategy strategy = GetStrategy(modeId);
                storedByMode.TryGetValue(modeId, out StoredAgentModeModelDefault? row);
                return new AgentModeModelDefaultDto(modeId, strategy.DisplayLabel, row?.ModelId);
            })
            .ToArray();
    }

    public async Task<Result<AgentModeModelDefaultDto>> UpdateAsync(
        string agentId,
        string mode,
        string? modelId,
        CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        string resolvedMode = mode.Trim();
        IAgentModeStrategy strategy;
        try
        {
            strategy = GetStrategy(resolvedMode);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<AgentModeModelDefaultDto>(Error.Validation("Mode.Unsupported", ex.Message));
        }

        string? resolvedModelId = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();

        if (resolvedModelId is not null)
        {
            bool enabled = await catalogService.IsEnabledModelAsync(agentId, resolvedModelId, cancellationToken);
            if (!enabled)
            {
                return Result.Failure<AgentModeModelDefaultDto>(
                    Error.Validation("Model.Unsupported", $"Model '{resolvedModelId}' is not available."));
            }
        }

        StoredAgentModeModelDefault updated = await store.UpsertAsync(
            agentId,
            strategy.ModeId,
            resolvedModelId,
            cancellationToken);

        return Result.Success(new AgentModeModelDefaultDto(
            updated.Mode,
            strategy.DisplayLabel,
            updated.ModelId));
    }

    public async Task<string?> ResolveAsync(string agentId, string mode, CancellationToken cancellationToken)
    {
        ValidateAgent(agentId);

        IAgentModeStrategy strategy = GetStrategy(mode);
        StoredAgentModeModelDefault? stored = await store.GetAsync(agentId, strategy.ModeId, cancellationToken);
        return stored?.ModelId;
    }

    private void ValidateAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent id is required.", nameof(agentId));
        }

        _ = adapterFactory.GetAdapter(agentId);
    }

    private IAgentModeStrategy GetStrategy(string modeId)
    {
        string resolvedMode = string.IsNullOrWhiteSpace(modeId) ? AgentModeIds.Default : modeId.Trim();

        IAgentModeStrategy? strategy = modeStrategies.FirstOrDefault(
            candidate => string.Equals(candidate.ModeId, resolvedMode, StringComparison.OrdinalIgnoreCase));

        if (strategy is null)
        {
            throw new InvalidOperationException($"No agent mode strategy registered for '{resolvedMode}'.");
        }

        return strategy;
    }
}
