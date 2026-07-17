using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.UpdateModeRuntimeDefault;

public static class UpdateModeRuntimeDefault
{
    public sealed record DefaultResponse(
        string Mode,
        string Label,
        string AgentId,
        string? ModelId,
        string? ContextSizeId,
        string? ReasoningEffortId,
        string? ApprovalPolicyId);

    public sealed record Command(
        string Mode,
        string AgentId,
        string? ModelId,
        string? ContextSizeId,
        string? ReasoningEffortId,
        string? ApprovalPolicyId) : ICommand<DefaultResponse>;

    internal sealed class Handler(IModeRuntimeDefaultService modeDefaultService)
        : ICommandHandler<Command, DefaultResponse>
    {
        public async Task<Result<DefaultResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ModeRuntimeDefaultDto> result = await modeDefaultService.UpdateAsync(
                command.Mode,
                command.AgentId,
                command.ModelId,
                command.ContextSizeId,
                command.ReasoningEffortId,
                command.ApprovalPolicyId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<DefaultResponse>(result.Error);
            }

            ModeRuntimeDefaultDto dto = result.Value;
            return Result.Success(new DefaultResponse(
                dto.Mode,
                dto.Label,
                dto.AgentId,
                dto.ModelId,
                dto.ContextSizeId,
                dto.ReasoningEffortId,
                dto.ApprovalPolicyId));
        }
    }

    public sealed record Request(
        string AgentId,
        string? ModelId,
        string? ContextSizeId,
        string? ReasoningEffortId,
        string? ApprovalPolicyId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Mode)
                .NotEmpty()
                .WithMessage("Mode is required.");

            RuleFor(command => command.AgentId)
                .NotEmpty()
                .WithMessage("Agent is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/agents/mode-defaults/{mode}", Handle)
                .WithName("UpdateModeRuntimeDefault")
                .WithTags("Agents")
                .Produces<DefaultResponse>();
        }

        private static async Task<IResult> Handle(
            string mode,
            Request request,
            ICommandHandler<Command, DefaultResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<DefaultResponse> result = await handler.Handle(
                new Command(
                    mode,
                    request.AgentId,
                    request.ModelId,
                    request.ContextSizeId,
                    request.ReasoningEffortId,
                    request.ApprovalPolicyId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
