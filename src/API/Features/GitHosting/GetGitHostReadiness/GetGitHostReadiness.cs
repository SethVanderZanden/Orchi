using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Git.Hosting;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.GitHosting.GetGitHostReadiness;

public static class GetGitHostReadiness
{
    public sealed record Response(
        string ProviderId,
        string Status,
        string Message,
        string? RequiredCli);

    public sealed record Query(Guid? ProjectId, string? WorkspacePath, string? Provider) : IQuery<Response>;

    internal sealed class Handler(IGitHostingFacade facade, IProjectStore projectStore)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            GitHostProvider provider = GitHostProvider.GitHub;
            string? workspacePath = query.WorkspacePath;

            if (query.ProjectId is Guid projectId)
            {
                Project? project = await projectStore.GetProjectAsync(projectId, cancellationToken);
                if (project is null)
                {
                    return Result.Failure<Response>(Error.NotFound($"Project '{projectId}' was not found."));
                }

                provider = project.GitHostProvider;
                workspacePath ??= project.Workspaces.FirstOrDefault(workspace => workspace.IsDefault)?.Path
                    ?? project.Workspaces.FirstOrDefault()?.Path;
            }

            if (!string.IsNullOrWhiteSpace(query.Provider))
            {
                if (!ProjectMapper.TryParseGitHost(query.Provider, out GitHostProvider parsed))
                {
                    return Result.Failure<Response>(
                        Error.Validation("GitHostProvider.Invalid", "Git host must be github or azureDevOps."));
                }

                provider = parsed;
            }

            GitHostReadiness readiness = await facade.GetReadinessAsync(provider, workspacePath, cancellationToken);
            return Result.Success(new Response(
                readiness.ProviderId,
                ToStatusName(readiness.Status),
                readiness.Message,
                readiness.RequiredCli));
        }

        private static string ToStatusName(GitHostReadinessStatus status) =>
            status switch
            {
                GitHostReadinessStatus.Ready => "ready",
                GitHostReadinessStatus.MissingCli => "missingCli",
                GitHostReadinessStatus.NotAuthenticated => "notAuthenticated",
                GitHostReadinessStatus.RepoNotDetected => "repoNotDetected",
                _ => status.ToString()
            };
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/git/hosting/readiness", Handle)
                .WithName("GetGitHostReadiness")
                .WithTags("GitHosting")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            Guid? projectId,
            string? workspacePath,
            string? provider,
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(
                new Query(projectId, workspacePath, provider),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
