using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.ListAgentContextSizes;

public static class ListAgentContextSizes
{
    public sealed record ContextSizeResponse(
        string Id,
        string Label,
        int TokenCount,
        bool IsEnabled,
        string Source);

    public sealed record Response(IReadOnlyList<ContextSizeResponse> ContextSizes);

    public sealed record Query(string AgentId, bool IncludeDisabled) : IQuery<Response>;

    internal sealed class Handler(IAgentContextSizeCatalogService catalogService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<AgentContextSizeDto> sizes = await catalogService.ListAsync(
                    query.AgentId,
                    query.IncludeDisabled,
                    cancellationToken);

                return Result.Success(new Response(
                    sizes.Select(size => new ContextSizeResponse(
                        size.Id,
                        size.Label,
                        size.TokenCount,
                        size.IsEnabled,
                        size.Source)).ToArray()));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Required", ex.Message));
            }
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agents/{agentId}/context-sizes", Handle)
                .WithName("ListAgentContextSizes")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            bool? includeDisabled,
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(
                new Query(agentId, includeDisabled ?? false),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
