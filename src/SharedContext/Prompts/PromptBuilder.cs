using System.Text;
using Microsoft.Extensions.Options;
using Orchi.SharedContext.Modes;
using Orchi.SharedContext.Storage;
using Orchi.SharedContext.Vectors;

namespace Orchi.SharedContext.Prompts;

internal sealed class PromptBuilder(
    IContextStore contextStore,
    IVectorStore vectorStore,
    IModeRuntime modeRuntime,
    ProjectRulesLoader rulesLoader,
    IOptions<SharedContextOptions> options) : IPromptBuilder
{
    private const int SafetyNetWithResume = 10;
    private const int SafetyNetWithoutResume = 50;

    public string BuildStablePrefix(string workspacePath, string modeInstructions)
    {
        string projectContext = rulesLoader.LoadStableProjectContext(workspacePath);
        if (string.IsNullOrWhiteSpace(projectContext))
        {
            return modeInstructions;
        }

        return $"{modeInstructions}\n\n{projectContext}";
    }

    public async Task<PromptBuildResult> BuildTurnAsync(
        PromptSessionContext context,
        string modeInstructions,
        CancellationToken cancellationToken)
    {
        string stable = BuildStablePrefix(context.WorkspacePath, modeInstructions);
        string dynamic = await BuildDynamicContextAsync(context, cancellationToken);
        return new PromptBuildResult(stable, dynamic);
    }

    private async Task<string> BuildDynamicContextAsync(
        PromptSessionContext context,
        CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        string? sessionSummary = await contextStore.GetSessionSummaryAsync(
            context.WorkspacePath,
            context.ChatId,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(sessionSummary))
        {
            parts.Add($"## Session summary\n\n{sessionSummary.Trim()}");
        }

        ModeTransitionContext? transition = modeRuntime.BuildTransitionContext(
            context.PreviousModeKey,
            context.ModeKey,
            context.ModeChangedAt);

        if (transition is not null)
        {
            parts.Add(
                $"## Mode transition\n\nMode changed from `{transition.PreviousModeKey}` to `{transition.NewModeKey}` at {transition.ChangedAt:O}.");
        }

        WorkspaceContext? workspace = await contextStore.GetWorkspaceAsync(context.WorkspacePath, cancellationToken);
        if (workspace?.GitBranch is not null)
        {
            parts.Add($"## Git\n\nBranch: `{workspace.GitBranch}`" +
                      (workspace.GitHead is not null ? $" @ `{workspace.GitHead}`" : string.Empty));
        }

        IReadOnlyList<ScoredChunk> retrieved = await vectorStore.SearchAsync(
            new VectorSearchQuery(context.WorkspacePath, context.CurrentUserContent, options.Value.RetrievalTopK),
            cancellationToken);

        if (retrieved.Count > 0)
        {
            var retrieval = new StringBuilder("## Relevant context\n");
            foreach (ScoredChunk chunk in retrieved)
            {
                retrieval.AppendLine();
                retrieval.AppendLine($"### {chunk.Title}");
                retrieval.AppendLine(chunk.Content);
            }

            parts.Add(retrieval.ToString().Trim());
        }

        string? history = FormatHistory(context);
        if (!string.IsNullOrWhiteSpace(history))
        {
            parts.Add(history);
        }

        if (!string.IsNullOrWhiteSpace(context.MiddleSection))
        {
            parts.Add(context.MiddleSection.Trim());
        }

        parts.Add(context.CurrentUserContent.Trim());
        return string.Join("\n\n", parts);
    }

    private static string? FormatHistory(PromptSessionContext context)
    {
        IReadOnlyList<PriorChatMessage> prior = context.PriorMessages
            .Where(message => message.Status is "complete" or "error")
            .Where(message => !string.Equals(message.Content.Trim(), context.CurrentUserContent.Trim(), StringComparison.Ordinal))
            .ToList();

        if (prior.Count == 0)
        {
            return null;
        }

        bool hasResume = !string.IsNullOrWhiteSpace(context.ExternalSessionId);
        int maxMessages = hasResume ? SafetyNetWithResume : SafetyNetWithoutResume;
        if (prior.Count > maxMessages)
        {
            prior = prior.TakeLast(maxMessages).ToList();
        }

        var builder = new StringBuilder("## Conversation so far\n");
        foreach (PriorChatMessage message in prior)
        {
            builder.AppendLine();
            builder.AppendLine($"**{message.Role}:** {message.Content}");
        }

        return builder.ToString().Trim();
    }
}
