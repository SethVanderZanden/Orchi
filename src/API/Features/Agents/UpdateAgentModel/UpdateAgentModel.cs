using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.UpdateAgentModel;

public static class UpdateAgentModel
{
    public sealed record ModelResponse(
        string Id,
        string Label,
        bool IsDefault,
        bool IsCurrent,
        bool IsEnabled,
        string Source);

    public sealed record Command(string AgentId, string ModelId, bool Enabled) : ICommand<ModelResponse>;

    internal sealed class Handler(IAgentModelCatalogService catalogService)
        : ICommandHandler<Command, ModelResponse>
    {
        public async Task<Result<ModelResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentModelDto> result = await catalogService.UpdateEnabledAsync(
                    command.AgentId,
                    command.ModelId,
                    command.Enabled,
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

    public sealed record Request(string ModelId, bool Enabled);

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
            app.MapPatch("/agents/{agentId}/models", Handle)
                .WithName("UpdateAgentModel")
                .WithTags("Agents")
                .Produces<ModelResponse>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            [FromBody] Request request,
            ICommandHandler<Command, ModelResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ModelResponse> result = await handler.Handle(
                new Command(agentId, request.ModelId, request.Enabled),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
