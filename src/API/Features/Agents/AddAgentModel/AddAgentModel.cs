using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.AddAgentModel;

public static class AddAgentModel
{
    public sealed record ModelResponse(
        string Id,
        string Label,
        bool IsDefault,
        bool IsCurrent,
        bool IsEnabled,
        string Source);

    public sealed record Command(string AgentId, string ModelId) : ICommand<ModelResponse>;

    internal sealed class Handler(IAgentModelCatalogService catalogService)
        : ICommandHandler<Command, ModelResponse>
    {
        public async Task<Result<ModelResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentModelDto> result = await catalogService.AddManualAsync(
                    command.AgentId,
                    command.ModelId,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Result.Failure<ModelResponse>(result.Error);
                }

                AgentModelDto model = result.Value;
                return Result.Success(new ModelResponse(
                    model.Id,
                    model.Label,
                    model.IsDefault,
                    model.IsCurrent,
                    model.IsEnabled,
                    model.Source));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<ModelResponse>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<ModelResponse>(Error.Validation("Agent.Required", ex.Message));
            }
        }
    }

    public sealed record Request(string ModelId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ModelId)
                .NotEmpty()
                .WithMessage("Model id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/agents/{agentId}/models", Handle)
                .WithName("AddAgentModel")
                .WithTags("Agents")
                .Produces<ModelResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            string agentId,
            Request request,
            ICommandHandler<Command, ModelResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ModelResponse> result = await handler.Handle(
                new Command(agentId, request.ModelId),
                cancellationToken);

            if (result.IsSuccess)
            {
                ModelResponse response = result.Value;
                return Results.Created(
                    $"/agents/{agentId}/models/{Uri.EscapeDataString(response.Id)}",
                    response);
            }

            return result.ToProblem();
        }
    }
}
