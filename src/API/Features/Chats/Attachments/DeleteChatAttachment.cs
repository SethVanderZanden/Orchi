using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Attachments;

namespace Orchi.Api.Features.Chats.Attachments;

public static class DeleteChatAttachment
{
    public sealed record Command(Guid ChatId, Guid AttachmentId) : ICommand;

    internal sealed class Handler(ChatAttachmentService attachmentService)
        : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken) =>
            attachmentService.DeleteStagedAsync(command.ChatId, command.AttachmentId, cancellationToken);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/chats/{chatId:guid}/attachments/{attachmentId:guid}", Handle)
                .WithName("DeleteChatAttachment")
                .WithTags("Chats");
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            Guid attachmentId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(chatId, attachmentId), cancellationToken);
            return result.IsFailure ? result.ToProblem() : Results.NoContent();
        }
    }
}
