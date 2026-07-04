using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.UpdateProject;

public static class UpdateProject
{
    public sealed record Command(Guid ProjectId, string Name) : ICommand<ProjectDetailResponse>;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command, ProjectDetailResponse>
    {
        public async Task<Result<ProjectDetailResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Project? project = await projectStore.UpdateProjectAsync(
                command.ProjectId,
                command.Name,
                cancellationToken);

            if (project is null)
            {
                return Result.Failure<ProjectDetailResponse>(
                    Error.NotFound($"Project '{command.ProjectId}' was not found."));
            }

            return Result.Success(ProjectMapper.ToDetail(project));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ProjectId)
                .NotEmpty()
                .WithMessage("Project id is required.");

            RuleFor(command => command.Name)
                .NotEmpty()
                .WithMessage("Project name is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/projects/{projectId:guid}", Handle)
                .WithName("UpdateProject")
                .WithTags("Projects")
                .Produces<ProjectDetailResponse>();
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            UpdateProjectRequest request,
            ICommandHandler<Command, ProjectDetailResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ProjectDetailResponse> result = await handler.Handle(
                new Command(projectId, request.Name),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
