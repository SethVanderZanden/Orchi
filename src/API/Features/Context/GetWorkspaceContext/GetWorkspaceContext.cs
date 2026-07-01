using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.SharedContext;
using Orchi.SharedContext.Indexing;
using Orchi.SharedContext.Storage;
using Orchi.SharedContext.Vectors;

namespace Orchi.Api.Features.Context.GetWorkspaceContext;

public static class GetWorkspaceContext
{
    public sealed record Query(string WorkspacePath) : IQuery<WorkspaceContextResponse>;

    public sealed record WorkspaceContextResponse(
        string WorkspacePath,
        DateTimeOffset? LastIndexedAt,
        string? GitBranch,
        string? GitHead,
        int IndexedFileCount,
        int SymbolCount);

    internal sealed class Handler(IContextStore contextStore) : IQueryHandler<Query, WorkspaceContextResponse>
    {
        public async Task<Result<WorkspaceContextResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.WorkspacePath))
            {
                return Result.Failure<WorkspaceContextResponse>(
                    Error.Validation("Workspace.Required", "Workspace path is required."));
            }

            string normalized = WorkspacePathNormalizer.Normalize(query.WorkspacePath);
            WorkspaceContext workspace = await contextStore.GetOrCreateWorkspaceAsync(normalized, cancellationToken);

            return Result.Success(new WorkspaceContextResponse(
                workspace.NormalizedPath,
                workspace.LastIndexedAt,
                workspace.GitBranch,
                workspace.GitHead,
                workspace.IndexedFileCount,
                workspace.SymbolCount));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/workspaces/context", Handle)
                .WithName("GetWorkspaceContext")
                .WithTags("Context")
                .Produces<WorkspaceContextResponse>();
        }

        private static async Task<IResult> Handle(
            string path,
            IQueryHandler<Query, WorkspaceContextResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<WorkspaceContextResponse> result = await handler.Handle(new Query(path), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        }
    }
}
