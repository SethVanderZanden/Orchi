using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orchi.Api.Infrastructure.Scripts;

public static class ScriptStepKinds
{
    public const string Shell = "shell";
    public const string GitCommit = "git.commit";
    public const string GitPush = "git.push";
    public const string GitMerge = "git.merge";
    public const string GitCreatePullRequest = "git.createPullRequest";
    public const string GitWorktree = "git.worktree";
}

public sealed record ScriptStepDto(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("command")] string? Command = null,
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("generateMessage")] bool GenerateMessage = false,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("body")] string? Body = null,
    [property: JsonPropertyName("sourceBranch")] string? SourceBranch = null,
    [property: JsonPropertyName("targetBranch")] string? TargetBranch = null,
    [property: JsonPropertyName("branch")] string? Branch = null,
    [property: JsonPropertyName("baseBranch")] string? BaseBranch = null,
    [property: JsonPropertyName("setUpstream")] bool SetUpstream = true);

public static class ScriptStepsSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IReadOnlyList<ScriptStepDto> Deserialize(string stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson))
        {
            return [];
        }

        List<ScriptStepDto>? steps = JsonSerializer.Deserialize<List<ScriptStepDto>>(stepsJson, Options);
        return steps ?? [];
    }

    public static string Serialize(IEnumerable<ScriptStepDto> steps) =>
        JsonSerializer.Serialize(steps.ToArray(), Options);

    public static bool TryValidate(string stepsJson, out string? error)
    {
        error = null;
        try
        {
            IReadOnlyList<ScriptStepDto> steps = Deserialize(stepsJson);
            if (steps.Count == 0)
            {
                error = "At least one script step is required.";
                return false;
            }

            foreach (ScriptStepDto step in steps)
            {
                if (!IsKnownKind(step.Kind))
                {
                    error = $"Unknown script step kind '{step.Kind}'.";
                    return false;
                }

                if (string.Equals(step.Kind, ScriptStepKinds.Shell, StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(step.Command))
                {
                    error = "shell steps require a command.";
                    return false;
                }
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid steps JSON: {ex.Message}";
            return false;
        }
    }

    private static bool IsKnownKind(string kind) =>
        kind is ScriptStepKinds.Shell
            or ScriptStepKinds.GitCommit
            or ScriptStepKinds.GitPush
            or ScriptStepKinds.GitMerge
            or ScriptStepKinds.GitCreatePullRequest
            or ScriptStepKinds.GitWorktree;
}
