using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Workspaces.UpdateWorkspace;

public static class UpdateWorkspace
{
    public sealed record Command(Guid WorkspaceId, string? Name, bool? IsDefault) : ICommand<WorkspaceResponse>;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command, WorkspaceResponse>
    {
        public async Task<Result<WorkspaceResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Workspace? workspace = await projectStore.UpdateWorkspaceAsync(
                command.WorkspaceId,
                command.Name,
                command.IsDefault,
                cancellationToken);

            if (workspace is null)
            {
                return Result.Failure<WorkspaceResponse>(
                    Error.NotFound($"Workspace '{command.WorkspaceId}' was not found."));
            }

            return Result.Success(ProjectMapper.ToWorkspaceResponse(workspace));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/workspaces/{workspaceId:guid}", Handle)
                .WithName("UpdateWorkspace")
                .WithTags("Workspaces")
                .Produces<WorkspaceResponse>();
        }

        private static async Task<IResult> Handle(
            Guid workspaceId,
            UpdateWorkspaceRequest request,
            ICommandHandler<Command, WorkspaceResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<WorkspaceResponse> result = await handler.Handle(
                new Command(workspaceId, request.Name, request.IsDefault),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
