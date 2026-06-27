using Orchi.Api.Common.Abstractions;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.SendMessage;

public static class SendMessage
{
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
            AgentSessionManager sessionManager,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Code = "Message.Required",
                    Message = "Message content is required."
                }, cancellationToken);
                return;
            }

            if (sessionManager.GetSession(chatId) is null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Code = "NotFound",
                    Message = $"Chat '{chatId}' was not found."
                }, cancellationToken);
                return;
            }

            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";
            httpContext.Response.ContentType = "text/event-stream";

            Guid assistantMessageId = Guid.Empty;

            await foreach (AgentEvent agentEvent in sessionManager.SendMessageAsync(
                               chatId,
                               request.Content.Trim(),
                               cancellationToken))
            {
                if (assistantMessageId == Guid.Empty)
                {
                    ChatSession? session = sessionManager.GetSession(chatId);
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
