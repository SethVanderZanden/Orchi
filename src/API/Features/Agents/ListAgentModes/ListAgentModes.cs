using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Features.Agents.ListAgentModes;

public static class ListAgentModes
{
    public sealed record ModeResponse(string Id, string Label, string? Description);

    public sealed record Query : IQuery<IReadOnlyList<ModeResponse>>;

    internal sealed class Handler(IEnumerable<IAgentModeStrategy> strategies)
        : IQueryHandler<Query, IReadOnlyList<ModeResponse>>
    {
        public Task<Result<IReadOnlyList<ModeResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ModeResponse> modes = strategies
                .Select(strategy => new ModeResponse(
                    strategy.ModeId,
                    strategy.DisplayLabel,
                    strategy.Description))
                .OrderBy(mode => mode.Label, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult(Result.Success(modes));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agents/modes", Handle)
                .WithName("ListAgentModes")
                .WithTags("Agents")
                .Produces<IReadOnlyList<ModeResponse>>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, IReadOnlyList<ModeResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ModeResponse>> result =
                await handler.Handle(new Query(), cancellationToken);

            return result.ToProblem();
        }
    }
}
