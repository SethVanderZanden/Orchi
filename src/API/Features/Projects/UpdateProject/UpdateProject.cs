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
    public sealed record Command(
        Guid ProjectId,
        string? Name,
        string? DefaultBaseBranch,
        string? DefaultWorktreeBranchPattern,
        string? GitHostProvider,
        bool? UseWorktreeOnKickoff) : ICommand<ProjectDetailResponse>;

    internal sealed class Handler(IProjectStore projectStore)
        : ICommandHandler<Command, ProjectDetailResponse>
    {
        public async Task<Result<ProjectDetailResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            GitHostProvider? host = null;
            if (!string.IsNullOrWhiteSpace(command.GitHostProvider))
            {
                if (!ProjectMapper.TryParseGitHost(command.GitHostProvider, out GitHostProvider parsed))
                {
                    return Result.Failure<ProjectDetailResponse>(
                        Error.Validation("GitHostProvider.Invalid", "Git host must be github or azureDevOps."));
                }

                host = parsed;
            }

            Project? project = await projectStore.UpdateProjectAsync(
                command.ProjectId,
                command.Name,
                command.DefaultBaseBranch,
                command.DefaultWorktreeBranchPattern,
                host,
                command.UseWorktreeOnKickoff,
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

            RuleFor(command => command)
                .Must(command =>
                    !string.IsNullOrWhiteSpace(command.Name)
                    || !string.IsNullOrWhiteSpace(command.DefaultBaseBranch)
                    || !string.IsNullOrWhiteSpace(command.DefaultWorktreeBranchPattern)
                    || !string.IsNullOrWhiteSpace(command.GitHostProvider)
                    || command.UseWorktreeOnKickoff is not null)
                .WithMessage("At least one project field must be provided.");
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
                new Command(
                    projectId,
                    request.Name,
                    request.DefaultBaseBranch,
                    request.DefaultWorktreeBranchPattern,
                    request.GitHostProvider,
                    request.UseWorktreeOnKickoff),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
