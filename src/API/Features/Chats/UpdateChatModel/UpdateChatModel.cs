using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.UpdateChatModel;

public static class UpdateChatModel
{
    public sealed record Command(Guid ChatId, string? ModelId) : ICommand<UpdateChatModelResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, UpdateChatModelResponse>
    {
        public async Task<Result<UpdateChatModelResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateModelAsync(
                command.ChatId,
                command.ModelId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<UpdateChatModelResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(new UpdateChatModelResponse(session.Id, session.ModelId));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}/model", Handle)
                .WithName("UpdateChatModel")
                .WithTags("Chats")
                .Produces<UpdateChatModelResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatModelRequest request,
            ICommandHandler<Command, UpdateChatModelResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<UpdateChatModelResponse> result = await handler.Handle(
                new Command(chatId, request.ModelId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
