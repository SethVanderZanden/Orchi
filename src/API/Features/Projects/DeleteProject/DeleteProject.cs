using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.DeleteProject;

public static class DeleteProject
{
    public sealed record Command(Guid ProjectId) : ICommand;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            bool deleted = await projectStore.DeleteProjectAsync(command.ProjectId, cancellationToken);
            if (!deleted)
            {
                return Result.Failure(Error.NotFound($"Project '{command.ProjectId}' was not found."));
            }

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/projects/{projectId:guid}", Handle)
                .WithName("DeleteProject")
                .WithTags("Projects");
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(projectId), cancellationToken);
            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return result.ToProblem();
        }
    }
}
