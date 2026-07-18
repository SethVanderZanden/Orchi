using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Features.Scripts.Shared;

public sealed record ScriptBindingRequest(
    string Event,
    string? ModeFilter = null,
    int Order = 0,
    bool Enabled = true,
    string OnError = "continue");

public sealed record ScriptBindingResponse(
    string Id,
    string ScriptId,
    string Event,
    string? ModeFilter,
    int Order,
    bool Enabled,
    string OnError);

public sealed record ScriptResponse(
    string Id,
    string Name,
    Guid? ProjectId,
    string StepsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ScriptBindingResponse> Bindings);

public sealed record CreateScriptRequest(
    string Name,
    Guid? ProjectId,
    string StepsJson,
    IReadOnlyList<ScriptBindingRequest>? Bindings = null);

public sealed record UpdateScriptRequest(
    string Name,
    string StepsJson,
    IReadOnlyList<ScriptBindingRequest>? Bindings = null);

public static class ScriptMapper
{
    public static ScriptResponse ToResponse(StoredScript script) =>
        new(
            script.Id,
            script.Name,
            script.ProjectId,
            script.StepsJson,
            script.CreatedAt,
            script.UpdatedAt,
            script.Bindings.Select(ToBindingResponse).ToArray());

    public static ScriptBindingResponse ToBindingResponse(StoredScriptBinding binding) =>
        new(
            binding.Id,
            binding.ScriptId,
            ToEventName(binding.Event),
            binding.ModeFilter,
            binding.Order,
            binding.Enabled,
            ToOnErrorName(binding.OnError));

    public static bool TryParseEvent(string value, out ScriptEventKind eventKind)
    {
        eventKind = default;
        if (string.Equals(value, "agentStart", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "AgentStart", StringComparison.OrdinalIgnoreCase))
        {
            eventKind = ScriptEventKind.AgentStart;
            return true;
        }

        if (string.Equals(value, "agentFinish", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "AgentFinish", StringComparison.OrdinalIgnoreCase))
        {
            eventKind = ScriptEventKind.AgentFinish;
            return true;
        }

        return false;
    }

    public static bool TryParseOnError(string value, out ScriptOnError onError)
    {
        onError = ScriptOnError.Continue;
        if (string.Equals(value, "continue", StringComparison.OrdinalIgnoreCase))
        {
            onError = ScriptOnError.Continue;
            return true;
        }

        if (string.Equals(value, "abortTurn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "AbortTurn", StringComparison.OrdinalIgnoreCase))
        {
            onError = ScriptOnError.AbortTurn;
            return true;
        }

        return false;
    }

    public static ScriptUpsertBinding[] ToUpsertBindings(IReadOnlyList<ScriptBindingRequest>? bindings)
    {
        if (bindings is null || bindings.Count == 0)
        {
            return [];
        }

        var result = new List<ScriptUpsertBinding>(bindings.Count);
        foreach (ScriptBindingRequest binding in bindings)
        {
            if (!TryParseEvent(binding.Event, out ScriptEventKind eventKind))
            {
                throw new ArgumentException($"Unknown script event '{binding.Event}'.");
            }

            if (!TryParseOnError(binding.OnError, out ScriptOnError onError))
            {
                throw new ArgumentException($"Unknown onError value '{binding.OnError}'.");
            }

            result.Add(new ScriptUpsertBinding(
                eventKind,
                binding.ModeFilter,
                binding.Order,
                binding.Enabled,
                onError));
        }

        return result.ToArray();
    }

    private static string ToEventName(ScriptEventKind eventKind) =>
        eventKind switch
        {
            ScriptEventKind.AgentStart => "agentStart",
            ScriptEventKind.AgentFinish => "agentFinish",
            _ => eventKind.ToString()
        };

    private static string ToOnErrorName(ScriptOnError onError) =>
        onError switch
        {
            ScriptOnError.Continue => "continue",
            ScriptOnError.AbortTurn => "abortTurn",
            _ => onError.ToString()
        };
}
