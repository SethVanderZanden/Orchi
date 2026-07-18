using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Scripts.Shared;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Features.Scripts.ListScripts;

public static class ListScripts
{
    public sealed record Query(Guid? ProjectId) : IQuery<IReadOnlyList<ScriptResponse>>;

    internal sealed class Handler(IScriptStore store)
        : IQueryHandler<Query, IReadOnlyList<ScriptResponse>>
    {
        public async Task<Result<IReadOnlyList<ScriptResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<StoredScript> scripts = await store.ListAsync(query.ProjectId, cancellationToken);
            return Result.Success<IReadOnlyList<ScriptResponse>>(
                scripts.Select(ScriptMapper.ToResponse).ToArray());
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/scripts", Handle)
                .WithName("ListScripts")
                .WithTags("Scripts")
                .Produces<IReadOnlyList<ScriptResponse>>();
        }

        private static async Task<IResult> Handle(
            Guid? projectId,
            IQueryHandler<Query, IReadOnlyList<ScriptResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ScriptResponse>> result = await handler.Handle(
                new Query(projectId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
