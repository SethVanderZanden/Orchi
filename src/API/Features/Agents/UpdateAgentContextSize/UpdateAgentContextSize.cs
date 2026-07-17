using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.UpdateAgentContextSize;

public static class UpdateAgentContextSize
{
    public sealed record ContextSizeResponse(
        string Id,
        string Label,
        int TokenCount,
        bool IsEnabled,
        string Source);

    public sealed record Command(string AgentId, string SizeId, bool IsEnabled) : ICommand<ContextSizeResponse>;

    internal sealed class Handler(IAgentContextSizeCatalogService catalogService)
        : ICommandHandler<Command, ContextSizeResponse>
    {
        public async Task<Result<ContextSizeResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentContextSizeDto> result = await catalogService.UpdateEnabledAsync(
                    command.AgentId,
                    command.SizeId,
                    command.IsEnabled,
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

    public sealed record Request(bool IsEnabled);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.SizeId)
                .NotEmpty()
                .WithMessage("Context size id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/agents/{agentId}/context-sizes/{sizeId}", Handle)
                .WithName("UpdateAgentContextSize")
                .WithTags("Agents")
                .Produces<ContextSizeResponse>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            string sizeId,
            Request request,
            ICommandHandler<Command, ContextSizeResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ContextSizeResponse> result = await handler.Handle(
                new Command(agentId, sizeId, request.IsEnabled),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
