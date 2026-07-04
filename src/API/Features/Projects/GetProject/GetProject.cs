using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.GetProject;

public static class GetProject
{
    public sealed record Query(Guid ProjectId) : IQuery<ProjectDetailResponse>;

    internal sealed class Handler(IProjectStore projectStore)
        : IQueryHandler<Query, ProjectDetailResponse>
    {
        public async Task<Result<ProjectDetailResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            Project? project = await projectStore.GetProjectAsync(query.ProjectId, cancellationToken);
            if (project is null)
            {
                return Result.Failure<ProjectDetailResponse>(
                    Error.NotFound($"Project '{query.ProjectId}' was not found."));
            }

            return Result.Success(ProjectMapper.ToDetail(project));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/projects/{projectId:guid}", Handle)
                .WithName("GetProject")
                .WithTags("Projects")
                .Produces<ProjectDetailResponse>();
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            IQueryHandler<Query, ProjectDetailResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ProjectDetailResponse> result = await handler.Handle(new Query(projectId), cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return result.ToProblem();
        }
    }
}
