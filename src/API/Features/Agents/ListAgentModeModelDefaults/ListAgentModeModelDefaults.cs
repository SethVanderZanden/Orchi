using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.ListAgentModeModelDefaults;

public static class ListAgentModeModelDefaults
{
    public sealed record DefaultResponse(string Mode, string Label, string? ModelId);

    public sealed record Response(IReadOnlyList<DefaultResponse> Defaults);

    public sealed record Query(string AgentId) : IQuery<Response>;

    internal sealed class Handler(IAgentModeModelDefaultService modeDefaultService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<AgentModeModelDefaultDto> defaults = await modeDefaultService.ListAsync(
                    query.AgentId,
                    cancellationToken);

                return Result.Success(new Response(
                    defaults.Select(ToResponse).ToArray()));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Unsupported", ex.Message));
            }
        }

        private static DefaultResponse ToResponse(AgentModeModelDefaultDto dto) =>
            new(dto.Mode, dto.Label, dto.ModelId);
    }

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.AgentId)
                .NotEmpty()
                .WithMessage("Agent id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agents/{agentId}/mode-model-defaults", Handle)
                .WithName("ListAgentModeModelDefaults")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(new Query(agentId), cancellationToken);
            return result.ToProblem();
        }
    }
}
