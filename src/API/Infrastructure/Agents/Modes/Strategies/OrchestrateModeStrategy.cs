using System.Text.Json;
using System.Text.RegularExpressions;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed partial class OrchestrateModeStrategy(PlanManager planManager) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Orchestrate;

    public Result<AgentTurnRequest> PrepareTurn(ChatSession session, string userContent, IPlanStore plans)
    {
        string prepared = $"{ModeInstructions.Orchestrate}\n\n---\n\n{userContent.Trim()}";
        return Result.Success(new AgentTurnRequest(prepared, ["--mode=plan"]));
    }

    public async ValueTask OnTurnCompletedAsync(
        ChatSession session,
        AgentCompletedEvent completed,
        IPlanStore plans,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(completed.FullText))
        {
            return;
        }

        if (!TryParseSubPlans(completed.FullText, out IReadOnlyList<SubPlanInput> subPlanInputs))
        {
            return;
        }

        PlanArtifact? existingPlan = plans.ListBySourceChat(session.Id).FirstOrDefault();
        if (existingPlan is null)
        {
            existingPlan = plans.Create(
                session.Id,
                "Orchestration plan",
                completed.FullText);
        }

        await planManager.UpsertSubPlansAsync(existingPlan.Id, subPlanInputs, cancellationToken);
    }

    public ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    internal static bool TryParseSubPlans(string text, out IReadOnlyList<SubPlanInput> subPlans)
    {
        subPlans = [];

        string? json = ExtractJsonBlock(text);
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
            foreach (JsonElement element in subPlansElement.EnumerateArray())
            {
                string title = element.TryGetProperty("title", out JsonElement titleElement)
                    ? titleElement.GetString() ?? string.Empty
                    : string.Empty;

                string content = element.TryGetProperty("contentMarkdown", out JsonElement contentElement)
                    ? contentElement.GetString() ?? string.Empty
                    : element.TryGetProperty("content", out JsonElement altContent)
                        ? altContent.GetString() ?? string.Empty
                        : string.Empty;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                parsed.Add(new SubPlanInput(Guid.Empty, title.Trim(), content.Trim()));
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

    private static string? ExtractJsonBlock(string text)
    {
        Match fenced = JsonFenceRegex().Match(text);
        if (fenced.Success)
        {
            return fenced.Groups["json"].Value;
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        return null;
    }

    [GeneratedRegex(@"```json\s*(?<json>\{.*?\})\s*```", RegexOptions.Singleline)]
    private static partial Regex JsonFenceRegex();
}
