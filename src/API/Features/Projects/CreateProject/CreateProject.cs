using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.CreateProject;

public static class CreateProject
{
    public sealed record Command(string Name, string DefaultWorkspacePath) : ICommand<CreateProjectResponse>;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command, CreateProjectResponse>
    {
        public async Task<Result<CreateProjectResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return Result.Failure<CreateProjectResponse>(
                    Error.Validation("Name.Required", "Project name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.DefaultWorkspacePath))
            {
                return Result.Failure<CreateProjectResponse>(
                    Error.Validation("DefaultWorkspacePath.Required", "Default workspace path is required."));
            }

            string fullPath = Path.GetFullPath(command.DefaultWorkspacePath);
            if (!Directory.Exists(fullPath))
            {
                return Result.Failure<CreateProjectResponse>(
                    Error.Validation("Workspace.NotFound", $"Workspace path does not exist: {fullPath}"));
            }

            ProjectCreateResult created = await projectStore.CreateProjectAsync(
                command.Name,
                fullPath,
                cancellationToken);

            return Result.Success(ProjectMapper.ToCreateResponse(created.Project, created.DefaultWorkspace));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .WithMessage("Project name is required.");

            RuleFor(command => command.DefaultWorkspacePath)
                .NotEmpty()
                .WithMessage("Default workspace path is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/projects", Handle)
                .WithName("CreateProject")
                .WithTags("Projects")
                .Produces<CreateProjectResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            CreateProjectRequest request,
            ICommandHandler<Command, CreateProjectResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<CreateProjectResponse> result = await handler.Handle(
                new Command(request.Name, request.DefaultWorkspacePath),
                cancellationToken);

            if (result.IsSuccess)
            {
                CreateProjectResponse response = result.Value;
                return Results.Created($"/projects/{response.Id}", response);
            }

            return result.ToProblem();
        }
    }
}
