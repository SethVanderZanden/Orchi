using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Attachments;
using Orchi.Api.Infrastructure.Agents.Attachments.Models;

namespace Orchi.Api.Features.Chats.Attachments;

public static class UploadChatAttachment
{
    public sealed record Command(Guid ChatId, IFormFile File) : ICommand<AttachmentResponse>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        ChatAttachmentService attachmentService)
        : ICommandHandler<Command, AttachmentResponse>
    {
        public async Task<Result<AttachmentResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            if (await sessionManager.GetOrLoadSessionAsync(command.ChatId, cancellationToken) is null)
            {
                return Result.Failure<AttachmentResponse>(
                    Error.NotFound($"Chat '{command.ChatId}' was not found."));
            }

            if (command.File.Length == 0)
            {
                return Result.Failure<AttachmentResponse>(
                    Error.Validation("Attachment.Empty", "File is empty."));
            }

            await using Stream stream = command.File.OpenReadStream();
            Result<StoredAttachment> staged = await attachmentService.StageUploadAsync(
                command.ChatId,
                command.File.FileName,
                command.File.ContentType,
                command.File.Length,
                stream,
                cancellationToken);

            if (staged.IsFailure)
            {
                return Result.Failure<AttachmentResponse>(staged.Error);
            }

            return Result.Success(ChatAttachmentMapper.ToResponse(staged.Value));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{chatId:guid}/attachments", Handle)
                .WithName("UploadChatAttachment")
                .WithTags("Chats")
                .DisableAntiforgery()
                .Produces<AttachmentResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            IFormFile file,
            ICommandHandler<Command, AttachmentResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<AttachmentResponse> result = await handler.Handle(
                new Command(chatId, file),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
