using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Attachments;

namespace Orchi.Api.Features.Chats.Attachments;

public static class GetChatAttachmentContent
{
    public sealed record Query(Guid ChatId, Guid AttachmentId) : IQuery<AttachmentContentResult>;

    public sealed record AttachmentContentResult(
        Stream Stream,
        string ContentType,
        string FileName);

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        ChatAttachmentService attachmentService)
        : IQueryHandler<Query, AttachmentContentResult>
    {
        public async Task<Result<AttachmentContentResult>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            ChatSession? session = await sessionManager.GetOrLoadSessionAsync(query.ChatId, cancellationToken);
            if (session is null)
            {
                return Result.Failure<AttachmentContentResult>(
                    Error.NotFound($"Chat '{query.ChatId}' was not found."));
            }

            Result<(Stream Stream, string ContentType, string FileName)> opened =
                await attachmentService.OpenReadAsync(
                    query.ChatId,
                    query.AttachmentId,
                    session.WorkspacePath,
                    cancellationToken);

            if (opened.IsFailure)
            {
                return Result.Failure<AttachmentContentResult>(opened.Error);
            }

            (Stream stream, string contentType, string fileName) = opened.Value;
            return Result.Success(new AttachmentContentResult(stream, contentType, fileName));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/{chatId:guid}/attachments/{attachmentId:guid}/content", Handle)
                .WithName("GetChatAttachmentContent")
                .WithTags("Chats");
        }

        private static async Task Handle(
            Guid chatId,
            Guid attachmentId,
            IQueryHandler<Query, AttachmentContentResult> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            Result<AttachmentContentResult> result = await handler.Handle(
                new Query(chatId, attachmentId),
                cancellationToken);

            if (result.IsFailure)
            {
                await httpContext.Response.WriteErrorAsync(result.Error, cancellationToken);
                return;
            }

            AttachmentContentResult content = result.Value;
            httpContext.Response.ContentType = content.ContentType;
            httpContext.Response.Headers.ContentDisposition =
                $"inline; filename=\"{content.FileName.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

            await using (content.Stream)
            {
                await content.Stream.CopyToAsync(httpContext.Response.Body, cancellationToken);
            }
        }
    }
}
