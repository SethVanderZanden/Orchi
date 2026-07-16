using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.SendMessage;

public static class SendMessage
{
    public sealed record SendMessageContext(Guid ChatId, string Content);

    public sealed record Command(Guid ChatId, string Content) : ICommand<SendMessageContext>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, SendMessageContext>
    {
        public async Task<Result<SendMessageContext>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.Content))
            {
                return Result.Failure<SendMessageContext>(
                    Error.Validation("Message.Required", "Message content is required."));
            }

            if (await sessionManager.GetOrLoadSessionAsync(command.ChatId, cancellationToken) is null)
            {
                return Result.Failure<SendMessageContext>(
                    Error.NotFound($"Chat '{command.ChatId}' was not found."));
            }

            return Result.Success(new SendMessageContext(command.ChatId, command.Content.Trim()));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{chatId:guid}/messages", Handle)
                .WithName("SendMessage")
                .WithTags("Chats");
        }

        private static async Task Handle(
            Guid chatId,
            SendMessageRequest request,
            ICommandHandler<Command, SendMessageContext> handler,
            AgentSessionManager sessionManager,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            Result<SendMessageContext> result = await handler.Handle(
                new Command(chatId, request.Content),
                cancellationToken);

            if (result.IsFailure)
            {
                await httpContext.Response.WriteErrorAsync(result.Error, cancellationToken);
                return;
            }

            SendMessageContext context = result.Value;

            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";
            httpContext.Response.ContentType = "text/event-stream";

            Guid assistantMessageId = Guid.Empty;

            await foreach (AgentEvent agentEvent in sessionManager.SendMessageAsync(
                               context.ChatId,
                               context.Content,
                               cancellationToken))
            {
                if (assistantMessageId == Guid.Empty)
                {
                    ChatSession? session = sessionManager.GetSession(context.ChatId);
                    ChatMessage? assistant = session?.Messages.LastOrDefault(message => message.Role == "assistant");
                    if (assistant is not null)
                    {
                        assistantMessageId = assistant.Id;
                    }
                }

                await ChatSseWriter.WriteAgentEventAsync(
                    httpContext.Response.Body,
                    agentEvent,
                    assistantMessageId,
                    cancellationToken);
            }
        }
    }
}
