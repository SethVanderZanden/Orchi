using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.ListAgentModels;

public static class ListAgentModels
{
    public sealed record ModelResponse(
        string Id,
        string Label,
        bool IsDefault,
        bool IsCurrent,
        bool IsEnabled,
        string Source);

    public sealed record Response(
        IReadOnlyList<ModelResponse> Models,
        DateTimeOffset? LastSyncedAt);

    public sealed record Query(string AgentId, bool IncludeDisabled) : IQuery<Response>;

    internal sealed class Handler(IAgentModelCatalogService catalogService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<AgentModelDto> models = await catalogService.ListAsync(
                    query.AgentId,
                    query.IncludeDisabled,
                    cancellationToken);

                DateTimeOffset? lastSyncedAt = await catalogService.GetLastSyncedAtAsync(
                    query.AgentId,
                    cancellationToken);

                return Result.Success(new Response(
                    models.Select(ToResponse).ToArray(),
                    lastSyncedAt));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Unsupported", ex.Message));
            }
        }

        private static ModelResponse ToResponse(AgentModelDto model) =>
            new(model.Id, model.Label, model.IsDefault, model.IsCurrent, model.IsEnabled, model.Source);
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
            app.MapGet("/agents/{agentId}/models", Handle)
                .WithName("ListAgentModels")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            bool includeDisabled,
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(
                new Query(agentId, includeDisabled),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
