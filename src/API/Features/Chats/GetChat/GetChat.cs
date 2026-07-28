using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Attachments;
using Orchi.Api.Infrastructure.Agents.Attachments.Models;

namespace Orchi.Api.Features.Chats.GetChat;

public static class GetChat
{
    public sealed record Query(Guid ChatId) : IQuery<ChatDetailResponse>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        ChatAttachmentService attachmentService)
        : IQueryHandler<Query, ChatDetailResponse>
    {
        public async Task<Result<ChatDetailResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            ChatSession? session = await sessionManager.GetOrLoadSessionAsync(query.ChatId, cancellationToken);

            if (session is null)
            {
                return Result.Failure<ChatDetailResponse>(
                    Error.NotFound($"Chat '{query.ChatId}' was not found."));
            }

            IReadOnlyList<StoredAttachment> attachments =
                await attachmentService.ListByChatAsync(query.ChatId, cancellationToken);

            Dictionary<Guid, List<AttachmentResponse>> byMessage = attachments
                .Where(attachment => attachment.MessageId is Guid messageId)
                .GroupBy(attachment => attachment.MessageId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(ChatAttachmentMapper.ToResponse).ToList());

            IReadOnlyDictionary<Guid, IReadOnlyList<AttachmentResponse>> attachmentsByMessage =
                byMessage.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<AttachmentResponse>)pair.Value);

            return Result.Success(ChatMapper.ToDetail(session, attachmentsByMessage));
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
