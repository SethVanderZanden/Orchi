using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.CreateWorktree;

public static class CreateWorktree
{
    public sealed record Command(
        Guid ProjectId,
        string? BaseBranch,
        string? BranchName,
        string? Name) : ICommand<WorkspaceResponse>;

    internal sealed class Handler(
        IProjectStore projectStore,
        IGitWorkspaceService gitWorkspaceService)
        : ICommandHandler<Command, WorkspaceResponse>
    {
        public async Task<Result<WorkspaceResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Project? project = await projectStore.GetProjectAsync(command.ProjectId, cancellationToken);
            if (project is null)
            {
                return Result.Failure<WorkspaceResponse>(
                    Error.NotFound($"Project '{command.ProjectId}' was not found."));
            }

            Workspace? primary = project.Workspaces.FirstOrDefault(workspace => workspace.IsDefault)
                ?? project.Workspaces.FirstOrDefault();

            if (primary is null)
            {
                return Result.Failure<WorkspaceResponse>(
                    Error.Validation("Workspace.Missing", "Project has no workspace to create a worktree from."));
            }

            string baseBranch = string.IsNullOrWhiteSpace(command.BaseBranch)
                ? project.DefaultBaseBranch
                : command.BaseBranch.Trim();

            string branchName = string.IsNullOrWhiteSpace(command.BranchName)
                ? WorktreeBranchPattern.Resolve(
                    project.DefaultWorktreeBranchPattern,
                    chatId: Guid.NewGuid(),
                    mode: null)
                : command.BranchName.Trim();

            try
            {
                GitWorktreeCreateResult created = await gitWorkspaceService.CreateWorktreeAsync(
                    primary.Path,
                    planId: branchName,
                    baseBranch,
                    branchName,
                    cancellationToken);

                WorkspaceCreateResult? workspace = await projectStore.CreateWorkspaceAsync(
                    project.Id,
                    created.Path,
                    string.IsNullOrWhiteSpace(command.Name) ? created.Branch : command.Name.Trim(),
                    WorkspaceKind.Worktree,
                    created.Branch,
                    created.BaseBranch,
                    cancellationToken);

                if (workspace is null)
                {
                    return Result.Failure<WorkspaceResponse>(
                        Error.NotFound($"Project '{command.ProjectId}' was not found."));
                }

                return Result.Success(ProjectMapper.ToWorkspaceResponse(workspace.Workspace));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<WorkspaceResponse>(Error.Validation("Worktree.Create", ex.Message));
            }
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ProjectId)
                .NotEmpty()
                .WithMessage("Project id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/projects/{projectId:guid}/worktrees", Handle)
                .WithName("CreateWorktree")
                .WithTags("Projects")
                .Produces<WorkspaceResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            CreateWorktreeRequest request,
            ICommandHandler<Command, WorkspaceResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<WorkspaceResponse> result = await handler.Handle(
                new Command(projectId, request.BaseBranch, request.BranchName, request.Name),
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

public sealed record CreateWorktreeRequest(
    string? BaseBranch = null,
    string? BranchName = null,
    string? Name = null);
