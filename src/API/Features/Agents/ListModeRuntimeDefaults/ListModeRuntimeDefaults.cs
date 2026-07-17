using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.ListModeRuntimeDefaults;

public static class ListModeRuntimeDefaults
{
    public sealed record DefaultResponse(
        string Mode,
        string Label,
        string AgentId,
        string? ModelId,
        string? ContextSizeId,
        string? ReasoningEffortId,
        string? ApprovalPolicyId);

    public sealed record Response(IReadOnlyList<DefaultResponse> Defaults);

    public sealed record Query : IQuery<Response>;

    internal sealed class Handler(IModeRuntimeDefaultService modeDefaultService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            IReadOnlyList<ModeRuntimeDefaultDto> defaults =
                await modeDefaultService.ListAsync(cancellationToken);

            return Result.Success(new Response(
                defaults.Select(row => new DefaultResponse(
                    row.Mode,
                    row.Label,
                    row.AgentId,
                    row.ModelId,
                    row.ContextSizeId,
                    row.ReasoningEffortId,
                    row.ApprovalPolicyId)).ToArray()));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agents/mode-defaults", Handle)
                .WithName("ListModeRuntimeDefaults")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(new Query(), cancellationToken);
            return result.ToProblem();
        }
    }
}
