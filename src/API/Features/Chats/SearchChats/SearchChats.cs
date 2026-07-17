using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Search;

namespace Orchi.Api.Features.Chats.SearchChats;

public static class SearchChats
{
    public sealed record Query(string? Q = null, int? Limit = null)
        : IQuery<IReadOnlyList<ChatSummaryResponse>>;

    internal sealed class Handler(IChatStore chatStore)
        : IQueryHandler<Query, IReadOnlyList<ChatSummaryResponse>>
    {
        public async Task<Result<IReadOnlyList<ChatSummaryResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            var criteria = new ChatSearchCriteria(Query: query.Q, Limit: query.Limit);
            IReadOnlyList<ChatSession> sessions = await chatStore.SearchAsync(criteria, cancellationToken);
            IReadOnlyList<ChatSummaryResponse> summaries = sessions
                .Select(ChatMapper.ToSummary)
                .ToArray();

            return Result.Success(summaries);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/search", Handle)
                .WithName("SearchChats")
                .WithTags("Chats")
                .Produces<IReadOnlyList<ChatSummaryResponse>>();
        }

        private static async Task<IResult> Handle(
            string? q,
            int? limit,
            IQueryHandler<Query, IReadOnlyList<ChatSummaryResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ChatSummaryResponse>> result =
                await handler.Handle(new Query(q, limit), cancellationToken);

            return result.ToProblem();
        }
    }
}
