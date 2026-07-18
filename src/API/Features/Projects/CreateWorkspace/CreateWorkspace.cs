using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.CreateWorkspace;

public static class CreateWorkspace
{
    public sealed record Command(
        Guid ProjectId,
        string Path,
        string? Name,
        string? Kind,
        string? Branch,
        string? BaseBranch) : ICommand<WorkspaceResponse>;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command, WorkspaceResponse>
    {
        public async Task<Result<WorkspaceResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            string fullPath = Path.GetFullPath(command.Path);
            if (!Directory.Exists(fullPath))
            {
                return Result.Failure<WorkspaceResponse>(
                    Error.Validation("Workspace.NotFound", $"Workspace path does not exist: {fullPath}"));
            }

            if (!TryParseKind(command.Kind, out WorkspaceKind kind))
            {
                return Result.Failure<WorkspaceResponse>(
                    Error.Validation("Workspace.Kind", "Workspace kind must be primary or worktree."));
            }

            try
            {
                WorkspaceCreateResult? created = await projectStore.CreateWorkspaceAsync(
                    command.ProjectId,
                    fullPath,
                    command.Name,
                    kind,
                    command.Branch,
                    command.BaseBranch,
                    cancellationToken);

                if (created is null)
                {
                    return Result.Failure<WorkspaceResponse>(
                        Error.NotFound($"Project '{command.ProjectId}' was not found."));
                }

                return Result.Success(ProjectMapper.ToWorkspaceResponse(created.Workspace));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<WorkspaceResponse>(Error.Validation("Workspace.Duplicate", ex.Message));
            }
        }

        private static bool TryParseKind(string? value, out WorkspaceKind kind)
        {
            kind = WorkspaceKind.Primary;
            if (string.IsNullOrWhiteSpace(value)
                || string.Equals(value, "primary", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "worktree", StringComparison.OrdinalIgnoreCase))
            {
                kind = WorkspaceKind.Worktree;
                return true;
            }

            return false;
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ProjectId)
                .NotEmpty()
                .WithMessage("Project id is required.");

            RuleFor(command => command.Path)
                .NotEmpty()
                .WithMessage("Workspace path is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/projects/{projectId:guid}/workspaces", Handle)
                .WithName("CreateWorkspace")
                .WithTags("Projects")
                .Produces<WorkspaceResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            CreateWorkspaceRequest request,
            ICommandHandler<Command, WorkspaceResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<WorkspaceResponse> result = await handler.Handle(
                new Command(
                    projectId,
                    request.Path,
                    request.Name,
                    request.Kind,
                    request.Branch,
                    request.BaseBranch),
                cancellationToken);

            if (result.IsSuccess)
            {
                WorkspaceResponse response = result.Value;
                return Results.Created($"/workspaces/{response.Id}", response);
            }

            return result.ToProblem();
        }
    }
}
