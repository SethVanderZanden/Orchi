using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Workspaces.DeleteWorkspace;

public static class DeleteWorkspace
{
    public sealed record Command(Guid WorkspaceId) : ICommand;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                bool deleted = await projectStore.DeleteWorkspaceAsync(command.WorkspaceId, cancellationToken);
                if (!deleted)
                {
                    return Result.Failure(Error.NotFound($"Workspace '{command.WorkspaceId}' was not found."));
                }

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation("Workspace.Last", ex.Message));
            }
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/workspaces/{workspaceId:guid}", Handle)
                .WithName("DeleteWorkspace")
                .WithTags("Workspaces");
        }

        private static async Task<IResult> Handle(
            Guid workspaceId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(workspaceId), cancellationToken);
            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return result.ToProblem();
        }
    }
}
