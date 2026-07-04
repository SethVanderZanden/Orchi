using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Projects;
using ProjectEntity = Orchi.Api.Entities.Project;

namespace Orchi.Api.Features.Projects.ListProjects;

public static class ListProjects
{
    public sealed record Query : IQuery<IReadOnlyList<ProjectSummaryResponse>>;

    internal sealed class Handler(IProjectStore projectStore)
        : IQueryHandler<Query, IReadOnlyList<ProjectSummaryResponse>>
    {
        public async Task<Result<IReadOnlyList<ProjectSummaryResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ProjectEntity> projects = await projectStore.ListProjectsAsync(cancellationToken);
            IReadOnlyList<ProjectSummaryResponse> response = projects
                .Select(ProjectMapper.ToSummary)
                .ToArray();

            return Result.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/projects", Handle)
                .WithName("ListProjects")
                .WithTags("Projects")
                .Produces<IReadOnlyList<ProjectSummaryResponse>>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, IReadOnlyList<ProjectSummaryResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ProjectSummaryResponse>> result =
                await handler.Handle(new Query(), cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return result.ToProblem();
        }
    }
}
