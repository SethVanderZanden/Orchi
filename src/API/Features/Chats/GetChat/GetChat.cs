using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.GetChat;

public static class GetChat
{
    public sealed record Query(Guid ChatId) : IQuery<ChatDetailResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : IQueryHandler<Query, ChatDetailResponse>
    {
        public Task<Result<ChatDetailResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            ChatSession? session = sessionManager.GetSession(query.ChatId);

            if (session is null)
            {
                return Task.FromResult(Result.Failure<ChatDetailResponse>(
                    Error.NotFound($"Chat '{query.ChatId}' was not found.")));
            }

            return Task.FromResult(Result.Success(ChatMapper.ToDetail(session)));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/{chatId:guid}", Handle)
                .WithName("GetChat")
                .WithTags("Chats")
                .Produces<ChatDetailResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            IQueryHandler<Query, ChatDetailResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ChatDetailResponse> result = await handler.Handle(new Query(chatId), cancellationToken);
            return result.ToProblem();
        }
    }
}
