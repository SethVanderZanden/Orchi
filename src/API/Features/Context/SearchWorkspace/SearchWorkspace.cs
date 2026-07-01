using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.SharedContext;
using Orchi.SharedContext.Vectors;

namespace Orchi.Api.Features.Context.SearchWorkspace;

public static class SearchWorkspace
{
    public sealed record Query(string WorkspacePath, string Q, int TopK = 8) : IQuery<SearchWorkspaceResponse>;

    public sealed record SearchWorkspaceResponse(IReadOnlyList<SearchResultItem> Results);

    public sealed record SearchResultItem(
        string Kind,
        string Title,
        string Content,
        string? SourcePath,
        double Score);

    internal sealed class Handler(IVectorStore vectorStore) : IQueryHandler<Query, SearchWorkspaceResponse>
    {
        public async Task<Result<SearchWorkspaceResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.WorkspacePath))
            {
                return Result.Failure<SearchWorkspaceResponse>(
                    Error.Validation("Workspace.Required", "Workspace path is required."));
            }

            if (string.IsNullOrWhiteSpace(query.Q))
            {
                return Result.Failure<SearchWorkspaceResponse>(
                    Error.Validation("Search.Required", "Search query is required."));
            }

            string normalized = WorkspacePathNormalizer.Normalize(query.WorkspacePath);
            IReadOnlyList<ScoredChunk> chunks = await vectorStore.SearchAsync(
                new VectorSearchQuery(normalized, query.Q, query.TopK),
                cancellationToken);

            IReadOnlyList<SearchResultItem> results = chunks
                .Select(chunk => new SearchResultItem(
                    chunk.Kind,
                    chunk.Title,
                    chunk.Content,
                    chunk.SourcePath,
                    chunk.Score))
                .ToList();

            return Result.Success(new SearchWorkspaceResponse(results));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/workspaces/search", Handle)
                .WithName("SearchWorkspace")
                .WithTags("Context")
                .Produces<SearchWorkspaceResponse>();
        }

        private static async Task<IResult> Handle(
            string path,
            string q,
            int? topK,
            IQueryHandler<Query, SearchWorkspaceResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<SearchWorkspaceResponse> result = await handler.Handle(
                new Query(path, q, topK ?? 8),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        }
    }
}
