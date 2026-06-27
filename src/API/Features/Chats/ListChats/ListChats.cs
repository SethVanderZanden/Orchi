using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.ListChats;

public static class ListChats
{
    public sealed record Query : IQuery<IReadOnlyList<ChatSummaryResponse>>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : IQueryHandler<Query, IReadOnlyList<ChatSummaryResponse>>
    {
        public Task<Result<IReadOnlyList<ChatSummaryResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ChatSummaryResponse> chats = sessionManager
                .ListSessions()
                .Select(ChatMapper.ToSummary)
                .ToArray();

            return Task.FromResult(Result.Success(chats));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats", Handle)
                .WithName("ListChats")
                .WithTags("Chats")
                .Produces<IReadOnlyList<ChatSummaryResponse>>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, IReadOnlyList<ChatSummaryResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ChatSummaryResponse>> result =
                await handler.Handle(new Query(), cancellationToken);

            return result.ToProblem();
        }
    }
}
