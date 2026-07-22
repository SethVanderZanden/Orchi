using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.ListProjectBranches;

public static class ListProjectBranches
{
    public sealed record Query(Guid ProjectId, bool Fetch) : IQuery<IReadOnlyList<ProjectBranchResponse>>;

    internal sealed class Handler(IProjectStore projectStore, IGitWorkspaceService gitWorkspaceService)
        : IQueryHandler<Query, IReadOnlyList<ProjectBranchResponse>>
    {
        public async Task<Result<IReadOnlyList<ProjectBranchResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            Project? project = await projectStore.GetProjectAsync(query.ProjectId, cancellationToken);
            if (project is null)
            {
                return Result.Failure<IReadOnlyList<ProjectBranchResponse>>(
                    Error.NotFound($"Project '{query.ProjectId}' was not found."));
            }

            Workspace? workspace = project.Workspaces.FirstOrDefault(item => item.IsDefault)
                ?? project.Workspaces.FirstOrDefault();

            if (workspace is null)
            {
                return Result.Failure<IReadOnlyList<ProjectBranchResponse>>(
                    Error.Validation("Workspace.Missing", "Project has no workspace."));
            }

            try
            {
                if (query.Fetch)
                {
                    await gitWorkspaceService.FetchAsync(workspace.Path, cancellationToken);
                }

                IReadOnlyList<GitBranchInfo> branches = await gitWorkspaceService.ListBranchesAsync(
                    workspace.Path,
                    cancellationToken,
                    includeRemotes: true);

                return Result.Success<IReadOnlyList<ProjectBranchResponse>>(
                    branches
                        .Select(branch => new ProjectBranchResponse(branch.Name, branch.IsCurrent, branch.IsRemote))
                        .ToArray());
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<IReadOnlyList<ProjectBranchResponse>>(
                    Error.Validation("Git.Branches", ex.Message));
            }
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/projects/{projectId:guid}/branches", Handle)
                .WithName("ListProjectBranches")
                .WithTags("Projects")
                .Produces<IReadOnlyList<ProjectBranchResponse>>();
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            bool? fetch,
            IQueryHandler<Query, IReadOnlyList<ProjectBranchResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ProjectBranchResponse>> result = await handler.Handle(
                new Query(projectId, fetch == true),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
