using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Features.Chats.UpdateChat;

public static class UpdateChat
{
    public sealed record Command(Guid ChatId, ChatMode Mode, Guid? AttachedPlanId)
        : ICommand<ChatSummaryResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, ChatSummaryResponse>
    {
        public async Task<Result<ChatSummaryResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateModeAsync(
                command.ChatId,
                command.Mode,
                command.AttachedPlanId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<ChatSummaryResponse>(result.Error);
            }

            return Result.Success(ChatMapper.ToSummary(result.Value));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ChatId)
                .NotEmpty()
                .WithMessage("Chat id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}", Handle)
                .WithName("UpdateChat")
                .WithTags("Chats")
                .Produces<ChatSummaryResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatRequest request,
            ICommandHandler<Command, ChatSummaryResponse> handler,
            CancellationToken cancellationToken)
        {
            if (!ChatModeParser.TryParse(request.Mode, out ChatMode mode))
            {
                return Results.BadRequest(new
                {
                    Code = "Mode.Invalid",
                    Message = $"Invalid chat mode '{request.Mode}'."
                });
            }

            Result<ChatSummaryResponse> result = await handler.Handle(
                new Command(chatId, mode, request.AttachedPlanId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
