using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed record ModeRuntimeDefaultDto(
    string Mode,
    string Label,
    string AgentId,
    string? ModelId,
    string? ContextSizeId,
    string? ReasoningEffortId,
    string? ApprovalPolicyId);

public sealed record ModeRuntimeResolution(
    string AgentId,
    string? ModelId,
    string? ContextSizeId,
    string? ReasoningEffortId,
    string? ApprovalPolicyId);

public interface IModeRuntimeDefaultService
{
    Task<IReadOnlyList<ModeRuntimeDefaultDto>> ListAsync(CancellationToken cancellationToken);

    Task<Result<ModeRuntimeDefaultDto>> UpdateAsync(
        string mode,
        string agentId,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        CancellationToken cancellationToken);

    Task<ModeRuntimeResolution> ResolveAsync(string mode, CancellationToken cancellationToken);

    /// <summary>
    /// Seeds or remaps per-mode agent defaults from the user's enabled agents.
    /// When <paramref name="seedAllModes"/> is true (first agent setup), every mode is
    /// written with the preferred enabled agent and null model/context/cli options.
    /// Later changes only remap modes whose agent was disabled.
    /// </summary>
    Task ApplyEnabledAgentsAsync(
        IReadOnlyList<string> enabledAgentIds,
        bool seedAllModes,
        CancellationToken cancellationToken);
}

public sealed class ModeRuntimeDefaultService(
    IModeRuntimeDefaultStore store,
    IAgentModelCatalogService modelCatalogService,
    IAgentContextSizeCatalogService contextSizeCatalogService,
    IAgentCliOptionCatalogService cliOptionCatalog,
    IAgentAdapterFactory adapterFactory,
    IEnumerable<IAgentModeStrategy> modeStrategies) : IModeRuntimeDefaultService
{
    private static readonly string[] ModeOrder =
    [
        AgentModeIds.Default,
        AgentModeIds.Orchestration,
        AgentModeIds.Review,
        AgentModeIds.Implementation
    ];

    public async Task<IReadOnlyList<ModeRuntimeDefaultDto>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<StoredModeRuntimeDefault> stored = await store.ListAsync(cancellationToken);
        var storedByMode = stored.ToDictionary(row => row.Mode, StringComparer.OrdinalIgnoreCase);

        return ModeOrder
            .Select(modeId =>
            {
                IAgentModeStrategy strategy = GetStrategy(modeId);
                if (!storedByMode.TryGetValue(modeId, out StoredModeRuntimeDefault? row))
                {
                    return new ModeRuntimeDefaultDto(
                        modeId,
                        strategy.DisplayLabel,
                        BuiltInDefaultAgentId(modeId),
                        null,
                        null,
                        null,
                        null);
                }

                return new ModeRuntimeDefaultDto(
                    modeId,
                    strategy.DisplayLabel,
                    row.AgentId,
                    row.ModelId,
                    row.ContextSizeId,
                    row.ReasoningEffortId,
                    row.ApprovalPolicyId);
            })
            .ToArray();
    }

    public async Task<Result<ModeRuntimeDefaultDto>> UpdateAsync(
        string mode,
        string agentId,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        CancellationToken cancellationToken)
    {
        string resolvedMode = mode.Trim();
        IAgentModeStrategy strategy;
        try
        {
            strategy = GetStrategy(resolvedMode);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ModeRuntimeDefaultDto>(Error.Validation("Mode.Unsupported", ex.Message));
        }

        string resolvedAgentId = agentId.Trim();
        try
        {
            _ = adapterFactory.GetAdapter(resolvedAgentId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ModeRuntimeDefaultDto>(Error.Validation("Agent.Unsupported", ex.Message));
        }

        string? resolvedModelId = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();
        string? resolvedContextSizeId = string.IsNullOrWhiteSpace(contextSizeId) ? null : contextSizeId.Trim();
        string? resolvedReasoningEffortId =
            string.IsNullOrWhiteSpace(reasoningEffortId) ? null : reasoningEffortId.Trim();
        string? resolvedApprovalPolicyId =
            string.IsNullOrWhiteSpace(approvalPolicyId) ? null : approvalPolicyId.Trim();

        if (resolvedModelId is not null)
        {
            bool enabled = await modelCatalogService.IsEnabledModelAsync(
                resolvedAgentId,
                resolvedModelId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ModeRuntimeDefaultDto>(
                    Error.Validation("Model.Unsupported", $"Model '{resolvedModelId}' is not available."));
            }
        }

        if (resolvedContextSizeId is not null)
        {
            bool enabled = await contextSizeCatalogService.IsEnabledSizeAsync(
                resolvedAgentId,
                resolvedContextSizeId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ModeRuntimeDefaultDto>(
                    Error.Validation(
                        "ContextSize.Unsupported",
                        $"Context size '{resolvedContextSizeId}' is not available."));
            }
        }

        if (resolvedReasoningEffortId is not null)
        {
            bool enabled = await cliOptionCatalog.IsEnabledOptionAsync(
                resolvedAgentId,
                AgentCliOptionKinds.ModelReasoningEffort,
                resolvedReasoningEffortId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ModeRuntimeDefaultDto>(
                    Error.Validation(
                        "ReasoningEffort.Unsupported",
                        $"Reasoning effort '{resolvedReasoningEffortId}' is not available."));
            }
        }

        if (resolvedApprovalPolicyId is not null)
        {
            bool enabled = await cliOptionCatalog.IsEnabledOptionAsync(
                resolvedAgentId,
                AgentCliOptionKinds.ApprovalPolicy,
                resolvedApprovalPolicyId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ModeRuntimeDefaultDto>(
                    Error.Validation(
                        "ApprovalPolicy.Unsupported",
                        $"Approval policy '{resolvedApprovalPolicyId}' is not available."));
            }
        }

        StoredModeRuntimeDefault updated = await store.UpsertAsync(
            strategy.ModeId,
            resolvedAgentId,
            resolvedModelId,
            resolvedContextSizeId,
            resolvedReasoningEffortId,
            resolvedApprovalPolicyId,
            cancellationToken);

        return Result.Success(new ModeRuntimeDefaultDto(
            updated.Mode,
            strategy.DisplayLabel,
            updated.AgentId,
            updated.ModelId,
            updated.ContextSizeId,
            updated.ReasoningEffortId,
            updated.ApprovalPolicyId));
    }

    public async Task<ModeRuntimeResolution> ResolveAsync(string mode, CancellationToken cancellationToken)
    {
        IAgentModeStrategy strategy = GetStrategy(mode);
        StoredModeRuntimeDefault? stored = await store.GetAsync(strategy.ModeId, cancellationToken);

        if (stored is null)
        {
            return new ModeRuntimeResolution(BuiltInDefaultAgentId(strategy.ModeId), null, null, null, null);
        }

        return new ModeRuntimeResolution(
            stored.AgentId,
            stored.ModelId,
            stored.ContextSizeId,
            stored.ReasoningEffortId,
            stored.ApprovalPolicyId);
    }

    public async Task ApplyEnabledAgentsAsync(
        IReadOnlyList<string> enabledAgentIds,
        bool seedAllModes,
        CancellationToken cancellationToken)
    {
        if (enabledAgentIds.Count == 0)
        {
            return;
        }

        IReadOnlyList<StoredModeRuntimeDefault> existing = await store.ListAsync(cancellationToken);
        var existingByMode = existing.ToDictionary(row => row.Mode, StringComparer.OrdinalIgnoreCase);

        foreach (string modeId in ModeOrder)
        {
            string preferredAgentId = PickDefaultAgentId(modeId, enabledAgentIds);

            if (!existingByMode.TryGetValue(modeId, out StoredModeRuntimeDefault? row))
            {
                await store.UpsertAsync(modeId, preferredAgentId, null, null, null, null, cancellationToken);
                continue;
            }

            bool agentStillEnabled = enabledAgentIds.Contains(row.AgentId, StringComparer.OrdinalIgnoreCase);
            if (seedAllModes || !agentStillEnabled)
            {
                await store.UpsertAsync(modeId, preferredAgentId, null, null, null, null, cancellationToken);
            }
        }
    }

    internal static string BuiltInDefaultAgentId(string modeId)
    {
        if (string.Equals(modeId, AgentModeIds.Default, StringComparison.OrdinalIgnoreCase))
        {
            return AgentIds.Cursor;
        }

        return AgentIds.Codex;
    }

    internal static string PickDefaultAgentId(string modeId, IReadOnlyList<string> enabledAgentIds)
    {
        if (enabledAgentIds.Count == 0)
        {
            return BuiltInDefaultAgentId(modeId);
        }

        string preferred = BuiltInDefaultAgentId(modeId);
        if (enabledAgentIds.Contains(preferred, StringComparer.OrdinalIgnoreCase))
        {
            return preferred;
        }

        return enabledAgentIds[0];
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
