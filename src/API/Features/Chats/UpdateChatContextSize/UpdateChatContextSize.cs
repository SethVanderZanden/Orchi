using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.UpdateChatContextSize;

public static class UpdateChatContextSize
{
    public sealed record Command(Guid ChatId, string? ContextSizeId) : ICommand<UpdateChatContextSizeResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, UpdateChatContextSizeResponse>
    {
        public async Task<Result<UpdateChatContextSizeResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateContextSizeAsync(
                command.ChatId,
                command.ContextSizeId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<UpdateChatContextSizeResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(new UpdateChatContextSizeResponse(session.Id, session.ContextSizeId));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}/context-size", Handle)
                .WithName("UpdateChatContextSize")
                .WithTags("Chats")
                .Produces<UpdateChatContextSizeResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatContextSizeRequest request,
            ICommandHandler<Command, UpdateChatContextSizeResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<UpdateChatContextSizeResponse> result = await handler.Handle(
                new Command(chatId, request.ContextSizeId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
