using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.UserPreferences;

public sealed record UserPreferenceDto(
    PostMessageBehavior PostMessageBehavior,
    IReadOnlyList<string> EnabledAgentIds,
    DateTimeOffset UpdatedAt);

public interface IUserPreferenceService
{
    Task<UserPreferenceDto> GetAsync(CancellationToken cancellationToken);

    Task<Result<UserPreferenceDto>> UpdateAsync(
        PostMessageBehavior? postMessageBehavior,
        IReadOnlyList<string>? enabledAgentIds,
        CancellationToken cancellationToken);
}

public sealed class UserPreferenceService(
    IUserPreferenceStore store,
    IAgentAdapterFactory adapterFactory,
    IModeRuntimeDefaultService modeRuntimeDefaultService) : IUserPreferenceService
{
    public async Task<UserPreferenceDto> GetAsync(CancellationToken cancellationToken)
    {
        StoredUserPreference stored = await store.GetOrCreateDefaultAsync(cancellationToken);
        return ToDto(stored);
    }

    public async Task<Result<UserPreferenceDto>> UpdateAsync(
        PostMessageBehavior? postMessageBehavior,
        IReadOnlyList<string>? enabledAgentIds,
        CancellationToken cancellationToken)
    {
        if (postMessageBehavior is null && enabledAgentIds is null)
        {
            return Result.Failure<UserPreferenceDto>(
                Error.Validation(
                    "UserPreferences.EmptyPatch",
                    "Provide postMessageBehavior and/or enabledAgentIds."));
        }

        if (postMessageBehavior is not null && !Enum.IsDefined(postMessageBehavior.Value))
        {
            return Result.Failure<UserPreferenceDto>(
                Error.Validation("PostMessageBehavior.Invalid", "Post-message behavior is invalid."));
        }

        IReadOnlyList<string>? normalizedAgents = null;
        if (enabledAgentIds is not null)
        {
            Result<IReadOnlyList<string>> agentsResult = ValidateEnabledAgents(enabledAgentIds);
            if (agentsResult.IsFailure)
            {
                return Result.Failure<UserPreferenceDto>(agentsResult.Error);
            }

            normalizedAgents = agentsResult.Value;
        }

        StoredUserPreference before = await store.GetOrCreateDefaultAsync(cancellationToken);
        bool seedAllModes = before.EnabledAgentIds.Count == 0 && normalizedAgents is not null;

        StoredUserPreference stored = await store.UpdateAsync(
            postMessageBehavior,
            normalizedAgents,
            cancellationToken);

        if (normalizedAgents is not null)
        {
            await modeRuntimeDefaultService.ApplyEnabledAgentsAsync(
                normalizedAgents,
                seedAllModes,
                cancellationToken);
        }

        return Result.Success(ToDto(stored));
    }

    private Result<IReadOnlyList<string>> ValidateEnabledAgents(IReadOnlyList<string> enabledAgentIds)
    {
        IReadOnlyList<string> normalized = EnabledAgentIdsSerializer.Normalize(enabledAgentIds);
        if (normalized.Count == 0)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation(
                    "EnabledAgents.Empty",
                    "Select at least one agent (Cursor or Codex)."));
        }

        HashSet<string> known = adapterFactory.ListAgentIds()
            .Select(id => id.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        foreach (string agentId in normalized)
        {
            if (known.Contains(agentId))
            {
                continue;
            }

            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation(
                    "EnabledAgents.Unknown",
                    $"Agent '{agentId}' is not available."));
        }

        return Result.Success(normalized);
    }

    private static UserPreferenceDto ToDto(StoredUserPreference stored) =>
        new(stored.PostMessageBehavior, stored.EnabledAgentIds, stored.UpdatedAt);
}
