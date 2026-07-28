using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Attachments;

namespace Orchi.Api.Features.Chats.Attachments;

public static class ListChatAttachments
{
    public sealed record Query(Guid ChatId) : IQuery<IReadOnlyList<AttachmentResponse>>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        ChatAttachmentService attachmentService)
        : IQueryHandler<Query, IReadOnlyList<AttachmentResponse>>
    {
        public async Task<Result<IReadOnlyList<AttachmentResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            if (await sessionManager.GetOrLoadSessionAsync(query.ChatId, cancellationToken) is null)
            {
                return Result.Failure<IReadOnlyList<AttachmentResponse>>(
                    Error.NotFound($"Chat '{query.ChatId}' was not found."));
            }

            var attachments = await attachmentService.ListByChatAsync(query.ChatId, cancellationToken);
            return Result.Success<IReadOnlyList<AttachmentResponse>>(
                attachments.Select(ChatAttachmentMapper.ToResponse).ToArray());
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/{chatId:guid}/attachments", Handle)
                .WithName("ListChatAttachments")
                .WithTags("Chats")
                .Produces<IReadOnlyList<AttachmentResponse>>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            IQueryHandler<Query, IReadOnlyList<AttachmentResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<AttachmentResponse>> result = await handler.Handle(
                new Query(chatId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
