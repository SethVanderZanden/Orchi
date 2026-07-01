using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.SharedContext;
using Orchi.SharedContext.Modes;
using Orchi.SharedContext.Prompts;
using Orchi.SharedContext.Storage;
using Orchi.SharedContext.Vectors;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

internal static class AgentPromptComposerTestFactory
{
    public static AgentPromptComposer Create(string workspacePath)
    {
        var options = Options.Create(new SharedContextOptions());
        IContextStore contextStore = new InMemoryContextStore();
        IVectorStore vectorStore = new KeywordVectorStore(contextStore);
        IModeRuntime modeRuntime = new ModeRuntime();
        var rulesLoader = new ProjectRulesLoader();
        IPromptBuilder promptBuilder = new PromptBuilder(
            contextStore,
            vectorStore,
            modeRuntime,
            rulesLoader,
            options);

        return new AgentPromptComposer(promptBuilder, modeRuntime);
    }

    private sealed class InMemoryContextStore : IContextStore
    {
        private readonly Dictionary<string, WorkspaceContext> _workspaces = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string Workspace, Guid ChatId), string> _summaries = new();

        public Task<WorkspaceContext> GetOrCreateWorkspaceAsync(string workspacePath, CancellationToken cancellationToken)
        {
            string normalized = WorkspacePathNormalizer.Normalize(workspacePath);
            if (_workspaces.TryGetValue(normalized, out WorkspaceContext? existing))
            {
                return Task.FromResult(existing);
            }

            var workspace = new WorkspaceContext(
                Guid.NewGuid(),
                normalized,
                null,
                null,
                null,
                0,
                0);

            _workspaces[normalized] = workspace;
            return Task.FromResult(workspace);
        }

        public Task<WorkspaceContext?> GetWorkspaceAsync(string workspacePath, CancellationToken cancellationToken) =>
            Task.FromResult(_workspaces.GetValueOrDefault(WorkspacePathNormalizer.Normalize(workspacePath)));

        public Task<IReadOnlyList<ContextChunk>> QueryAsync(ContextQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContextChunk>>([]);

        public Task UpsertAsync(ContextUpsert upsert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string?> GetSessionSummaryAsync(string workspacePath, Guid chatId, CancellationToken cancellationToken)
        {
            _summaries.TryGetValue((WorkspacePathNormalizer.Normalize(workspacePath), chatId), out string? summary);
            return Task.FromResult(summary);
        }

        public Task UpsertSessionSummaryAsync(
            string workspacePath,
            Guid chatId,
            string summary,
            string status,
            CancellationToken cancellationToken)
        {
            _summaries[(WorkspacePathNormalizer.Normalize(workspacePath), chatId)] = summary;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, string>> GetFileHashesAsync(
            string workspacePath,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }
}
