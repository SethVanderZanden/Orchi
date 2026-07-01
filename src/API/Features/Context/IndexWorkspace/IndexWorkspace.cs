using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.SharedContext;
using Orchi.SharedContext.Indexing;

namespace Orchi.Api.Features.Context.IndexWorkspace;

public static class IndexWorkspace
{
    public sealed record Command(string WorkspacePath, bool FullRebuild = false) : ICommand<IndexWorkspaceResponse>;

    public sealed record IndexWorkspaceResponse(
        int FilesScanned,
        int FilesUpdated,
        int SymbolsExtracted,
        string? GitBranch,
        string? GitHead);

    internal sealed class Handler(IProjectIndexer indexer) : ICommandHandler<Command, IndexWorkspaceResponse>
    {
        public async Task<Result<IndexWorkspaceResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            {
                return Result.Failure<IndexWorkspaceResponse>(
                    Error.Validation("Workspace.Required", "Workspace path is required."));
            }

            string normalized = WorkspacePathNormalizer.Normalize(command.WorkspacePath);
            if (!Directory.Exists(normalized))
            {
                return Result.Failure<IndexWorkspaceResponse>(
                    Error.Validation("Workspace.NotFound", $"Workspace path does not exist: {normalized}"));
            }

            IndexResult result = await indexer.IndexAsync(
                normalized,
                new IndexOptions(command.FullRebuild),
                cancellationToken);

            return Result.Success(new IndexWorkspaceResponse(
                result.FilesScanned,
                result.FilesUpdated,
                result.SymbolsExtracted,
                result.GitBranch,
                result.GitHead));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/workspaces/index", Handle)
                .WithName("IndexWorkspace")
                .WithTags("Context")
                .Produces<IndexWorkspaceResponse>();
        }

        private static async Task<IResult> Handle(
            IndexWorkspaceRequest request,
            ICommandHandler<Command, IndexWorkspaceResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<IndexWorkspaceResponse> result = await handler.Handle(
                new Command(request.WorkspacePath, request.FullRebuild),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        }
    }

    public sealed record IndexWorkspaceRequest(string WorkspacePath, bool FullRebuild = false);
}
