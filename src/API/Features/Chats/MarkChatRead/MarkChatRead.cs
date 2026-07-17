using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.MarkChatRead;

public static class MarkChatRead
{
    public sealed record Command(Guid ChatId) : ICommand<ChatSummaryResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, ChatSummaryResponse>
    {
        public async Task<Result<ChatSummaryResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.MarkReadAsync(
                command.ChatId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<ChatSummaryResponse>(result.Error);
            }

            return Result.Success(ChatMapper.ToSummary(result.Value));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{chatId:guid}/read", Handle)
                .WithName("MarkChatRead")
                .WithTags("Chats")
                .Produces<ChatSummaryResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            ICommandHandler<Command, ChatSummaryResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ChatSummaryResponse> result = await handler.Handle(
                new Command(chatId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
