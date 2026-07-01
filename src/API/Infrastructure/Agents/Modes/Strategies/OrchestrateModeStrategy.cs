using System.Text.Json;
using System.Text.RegularExpressions;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed partial class OrchestrateModeStrategy(
    AgentPromptComposer promptComposer,
    PlanManager planManager) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Orchestrate;

    public ValueTask<Result<AgentTurnRequest>> PrepareTurnAsync(
        ChatSession session,
        string userContent,
        IPlanStore plans,
        CancellationToken cancellationToken) =>
        new(promptComposer.ComposeAsync(session, userContent, ModeInstructions.Orchestrate, null, cancellationToken));

    public async ValueTask OnTurnCompletedAsync(
        ChatSession session,
        AgentCompletedEvent completed,
        IPlanStore plans,
        CancellationToken cancellationToken)
    {
        if (!TryParseSubPlans(completed.FullText, out IReadOnlyList<SubPlanInput> subPlans))
        {
            return;
        }

        PlanArtifact? existing = plans.ListBySourceChat(session.Id).FirstOrDefault();
        PlanArtifact plan;
        if (existing is null)
        {
            Result<PlanArtifact> created = planManager.CreatePlan(
                session.Id,
                "Orchestration",
                completed.FullText);

            if (created.IsFailure)
            {
                return;
            }

            plan = created.Value;
        }
        else
        {
            plan = existing;
        }

        await planManager.UpsertSubPlansAsync(plan.Id, subPlans, cancellationToken);
    }

    public ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public static bool TryParseSubPlans(string text, out IReadOnlyList<SubPlanInput> subPlans)
    {
        subPlans = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string? json = ExtractJsonPayload(text);
        if (json is null)
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("subPlans", out JsonElement subPlansElement) ||
                subPlansElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var parsed = new List<SubPlanInput>();
            foreach (JsonElement item in subPlansElement.EnumerateArray())
            {
                string title = item.TryGetProperty("title", out JsonElement titleElement)
                    ? titleElement.GetString() ?? string.Empty
                    : string.Empty;

                string content = item.TryGetProperty("contentMarkdown", out JsonElement contentElement)
                    ? contentElement.GetString() ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                Guid id = item.TryGetProperty("id", out JsonElement idElement) &&
                          idElement.TryGetGuid(out Guid parsedId)
                    ? parsedId
                    : Guid.Empty;

                parsed.Add(new SubPlanInput(id, title.Trim(), content.Trim()));
            }

            if (parsed.Count == 0)
            {
                return false;
            }

            subPlans = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractJsonPayload(string text)
    {
        Match fenced = FencedJsonRegex().Match(text);
        if (fenced.Success)
        {
            return fenced.Groups["json"].Value.Trim();
        }

        int start = text.IndexOf("{\"subPlans\"", StringComparison.Ordinal);
        if (start < 0)
        {
            start = text.IndexOf("{ \"subPlans\"", StringComparison.Ordinal);
        }

        if (start < 0)
        {
            return null;
        }

        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text[start..(i + 1)];
                }
            }
        }

        return null;
    }

    [GeneratedRegex(@"```json\s*(?<json>\{[\s\S]*?\})\s*```", RegexOptions.Compiled)]
    private static partial Regex FencedJsonRegex();
}
