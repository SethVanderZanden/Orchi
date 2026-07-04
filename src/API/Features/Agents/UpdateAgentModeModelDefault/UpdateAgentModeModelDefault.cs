using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.UpdateAgentModeModelDefault;

public static class UpdateAgentModeModelDefault
{
    public sealed record DefaultResponse(string Mode, string Label, string? ModelId);

    public sealed record Command(string AgentId, string Mode, string? ModelId) : ICommand<DefaultResponse>;

    internal sealed class Handler(IAgentModeModelDefaultService modeDefaultService)
        : ICommandHandler<Command, DefaultResponse>
    {
        public async Task<Result<DefaultResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentModeModelDefaultDto> result = await modeDefaultService.UpdateAsync(
                    command.AgentId,
                    command.Mode,
                    command.ModelId,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Result.Failure<DefaultResponse>(result.Error);
                }

                AgentModeModelDefaultDto dto = result.Value;
                return Result.Success(new DefaultResponse(dto.Mode, dto.Label, dto.ModelId));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<DefaultResponse>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<DefaultResponse>(Error.Validation("Agent.Required", ex.Message));
            }
        }
    }

    public sealed record Request(string? ModelId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Mode)
                .NotEmpty()
                .WithMessage("Mode is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/agents/{agentId}/mode-model-defaults/{mode}", Handle)
                .WithName("UpdateAgentModeModelDefault")
                .WithTags("Agents")
                .Produces<DefaultResponse>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            string mode,
            Request request,
            ICommandHandler<Command, DefaultResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<DefaultResponse> result = await handler.Handle(
                new Command(agentId, mode, request.ModelId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
