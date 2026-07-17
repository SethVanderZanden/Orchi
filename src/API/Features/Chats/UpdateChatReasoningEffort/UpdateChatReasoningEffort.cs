using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.UpdateChatReasoningEffort;

public static class UpdateChatReasoningEffort
{
    public sealed record Command(Guid ChatId, string? ReasoningEffortId)
        : ICommand<UpdateChatReasoningEffortResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, UpdateChatReasoningEffortResponse>
    {
        public async Task<Result<UpdateChatReasoningEffortResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateReasoningEffortAsync(
                command.ChatId,
                command.ReasoningEffortId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<UpdateChatReasoningEffortResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(
                new UpdateChatReasoningEffortResponse(session.Id, session.ReasoningEffortId));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}/reasoning-effort", Handle)
                .WithName("UpdateChatReasoningEffort")
                .WithTags("Chats")
                .Produces<UpdateChatReasoningEffortResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatReasoningEffortRequest request,
            ICommandHandler<Command, UpdateChatReasoningEffortResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<UpdateChatReasoningEffortResponse> result = await handler.Handle(
                new Command(chatId, request.ReasoningEffortId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
