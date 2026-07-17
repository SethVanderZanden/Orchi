using Orchi.Api.Common.Abstractions;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Agents.ListAgents;

public static class ListAgents
{
    public sealed record AgentResponse(string Id, string Label);

    public sealed record Response(IReadOnlyList<AgentResponse> Agents);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agents", Handle)
                .WithName("ListAgents")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static IResult Handle(IAgentAdapterFactory adapterFactory)
        {
            IReadOnlyList<AgentResponse> agents = adapterFactory.ListAgentIds()
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .Select(id => new AgentResponse(id, AgentIds.DisplayLabel(id)))
                .ToArray();

            return Results.Ok(new Response(agents));
        }
    }
}
