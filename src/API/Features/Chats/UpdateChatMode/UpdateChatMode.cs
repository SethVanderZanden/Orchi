using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.UpdateChatMode;

public static class UpdateChatMode
{
    public sealed record Command(Guid ChatId, string Mode) : ICommand<UpdateChatModeResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, UpdateChatModeResponse>
    {
        public async Task<Result<UpdateChatModeResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateModeAsync(
                command.ChatId,
                command.Mode,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<UpdateChatModeResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(new UpdateChatModeResponse(session.Id, session.Mode));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ChatId)
                .NotEmpty()
                .WithMessage("Chat id is required.");

            RuleFor(command => command.Mode)
                .NotEmpty()
                .WithMessage("Mode is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}/mode", Handle)
                .WithName("UpdateChatMode")
                .WithTags("Chats")
                .Produces<UpdateChatModeResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatModeRequest request,
            ICommandHandler<Command, UpdateChatModeResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<UpdateChatModeResponse> result = await handler.Handle(
                new Command(chatId, request.Mode),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
