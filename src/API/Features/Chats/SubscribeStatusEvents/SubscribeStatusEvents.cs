using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.SubscribeStatusEvents;

public static class SubscribeStatusEvents
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/status/events", Handle)
                .WithName("SubscribeChatStatusEvents")
                .WithTags("Chats");
        }

        private static async Task Handle(
            IChatStatusService chatStatusService,
            ChatStatusEventHub eventHub,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";
            httpContext.Response.ContentType = "text/event-stream";

            IReadOnlyList<ChatStatusSnapshotItem> snapshot =
                await chatStatusService.ListStatusesAsync(cancellationToken);

            await ChatSseWriter.WriteEventAsync(
                httpContext.Response.Body,
                "snapshot",
                snapshot,
                cancellationToken);

            (Guid subscriptionId, System.Threading.Channels.ChannelReader<ChatStatusChangedEvent> reader) =
                eventHub.Subscribe();

            try
            {
                await foreach (ChatStatusChangedEvent statusEvent in reader.ReadAllAsync(cancellationToken))
                {
                    await ChatSseWriter.WriteEventAsync(
                        httpContext.Response.Body,
                        "status",
                        new ChatStatusSsePayload(statusEvent.ChatId, statusEvent.Status),
                        cancellationToken);
                }
            }
            finally
            {
                eventHub.Unsubscribe(subscriptionId);
            }
        }
    }
}
