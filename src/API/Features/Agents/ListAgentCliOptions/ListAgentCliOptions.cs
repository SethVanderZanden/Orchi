using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.ListAgentCliOptions;

public static class ListAgentCliOptions
{
    public sealed record CliOptionResponse(
        string Kind,
        string Id,
        string Label,
        string CliValue,
        bool IsEnabled,
        string Source);

    public sealed record Response(IReadOnlyList<CliOptionResponse> Options);

    public sealed record Query(string AgentId, string Kind, bool IncludeDisabled) : IQuery<Response>;

    internal sealed class Handler(IAgentCliOptionCatalogService catalogService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<AgentCliOptionDto> options = await catalogService.ListAsync(
                    query.AgentId,
                    query.Kind,
                    query.IncludeDisabled,
                    cancellationToken);

                return Result.Success(new Response(
                    options.Select(option => new CliOptionResponse(
                        option.Kind,
                        option.Id,
                        option.Label,
                        option.CliValue,
                        option.IsEnabled,
                        option.Source)).ToArray()));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Response>(Error.Validation("CliOption.Kind", ex.Message));
            }
        }
    }

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.AgentId)
                .NotEmpty()
                .WithMessage("Agent id is required.");

            RuleFor(query => query.Kind)
                .NotEmpty()
                .WithMessage("Kind is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/agents/{agentId}/cli-options/{kind}", Handle)
                .WithName("ListAgentCliOptions")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            string kind,
            bool? includeDisabled,
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(
                new Query(agentId, kind, includeDisabled ?? false),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
