using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.AddAgentContextSize;

public static class AddAgentContextSize
{
    public sealed record ContextSizeResponse(
        string Id,
        string Label,
        int TokenCount,
        bool IsEnabled,
        string Source);

    public sealed record Command(
        string AgentId,
        string SizeId,
        string Label,
        int TokenCount) : ICommand<ContextSizeResponse>;

    internal sealed class Handler(IAgentContextSizeCatalogService catalogService)
        : ICommandHandler<Command, ContextSizeResponse>
    {
        public async Task<Result<ContextSizeResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentContextSizeDto> result = await catalogService.AddManualAsync(
                    command.AgentId,
                    command.SizeId,
                    command.Label,
                    command.TokenCount,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Result.Failure<ContextSizeResponse>(result.Error);
                }

                AgentContextSizeDto size = result.Value;
                return Result.Success(new ContextSizeResponse(
                    size.Id,
                    size.Label,
                    size.TokenCount,
                    size.IsEnabled,
                    size.Source));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<ContextSizeResponse>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<ContextSizeResponse>(Error.Validation("Agent.Required", ex.Message));
            }
        }
    }

    public sealed record Request(string SizeId, string? Label, int TokenCount);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.SizeId)
                .NotEmpty()
                .WithMessage("Context size id is required.");

            RuleFor(command => command.TokenCount)
                .GreaterThan(0)
                .WithMessage("Token count must be greater than zero.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/agents/{agentId}/context-sizes", Handle)
                .WithName("AddAgentContextSize")
                .WithTags("Agents")
                .Produces<ContextSizeResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            string agentId,
            Request request,
            ICommandHandler<Command, ContextSizeResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ContextSizeResponse> result = await handler.Handle(
                new Command(agentId, request.SizeId, request.Label ?? request.SizeId, request.TokenCount),
                cancellationToken);

            if (result.IsSuccess)
            {
                ContextSizeResponse response = result.Value;
                return Results.Created(
                    $"/agents/{agentId}/context-sizes/{Uri.EscapeDataString(response.Id)}",
                    response);
            }

            return result.ToProblem();
        }
    }
}
